using System.Globalization;
using System.Text;
using Chronify.Localization;
using Chronify.Services;
using Chronify.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Chronify.Tests;

// Культура спільна для процесу, тому ці тести не запускаємо паралельно з іншими.
[CollectionDefinition("Localization", DisableParallelization = true)]
public class LocalizationCollection
{
}

[Collection("Localization")]
public class LocalizationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ChronifyLanguages", Guid.NewGuid().ToString("N"));
    private readonly string _previousLanguage = LocalizationService.Current.LanguageCode;

    public LocalizationTests()
    {
        Directory.CreateDirectory(_directory);
    }

    private AppDbContext CreateContext()
    {
        string path = Path.Combine(_directory, "languages.db");
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={path}").Options);
    }

    [Fact]
    public void DictionariesContainTheSameKeysAndCompatiblePlaceholders()
    {
        var expectedKeys = RussianTexts.Values.Keys.Order().ToArray();
        foreach (var dictionary in new[] { UkrainianTexts.Values, EnglishTexts.Values })
        {
            Assert.Equal(expectedKeys, dictionary.Keys.Order());
            foreach (string key in expectedKeys)
            {
                Assert.False(string.IsNullOrWhiteSpace(dictionary[key]), key);
                Assert.Equal(
                    CompositeFormat.Parse(RussianTexts.Values[key]).MinimumArgumentCount,
                    CompositeFormat.Parse(dictionary[key]).MinimumArgumentCount
                );
            }
        }
    }

    [Theory]
    [InlineData("ru", "Сохранить", "45 мин", "ru-RU")]
    [InlineData("uk", "Зберегти", "45 хв", "uk-UA")]
    [InlineData("en", "Save", "45 min", "en-GB")]
    public void LanguageChangesTextsDurationsAndCulture(string code, string saveText, string duration, string culture)
    {
        LocalizationService.Current.ChangeLanguage(code);
        Assert.Equal(saveText, Texts.Get("Common.Save"));
        Assert.Equal(duration, DurationText.From(45));
        Assert.Equal(culture, CultureInfo.CurrentCulture.Name);
        Assert.Equal(culture, LocalizationService.Current.Culture.Name);
        Assert.Equal(new DateTime(2026, 11, 19).ToString("d", CultureInfo.GetCultureInfo(culture)), Texts.Date(new DateTime(2026, 11, 19)));
    }

    [Fact]
    public void LanguagePreferencesAreIsolatedAndDoNotOverwriteTimeLimits()
    {
        var first = new UserAccount { NormalizedEmail = "FIRST@TEST.LOCAL" };
        var second = new UserAccount { NormalizedEmail = "SECOND@TEST.LOCAL" };
        using (var context = CreateContext())
        {
            DatabaseInitializer.Initialize(context);
            context.Users.AddRange(first, second);
            context.SaveChanges();
        }
        var firstPreferences = new UserPreferencesRepository(first.Id, CreateContext);
        var secondPreferences = new UserPreferencesRepository(second.Id, CreateContext);
        firstPreferences.Save(350, 95);
        firstPreferences.SaveLanguage("uk");
        secondPreferences.SaveLanguage("en");
        Assert.Equal("uk", firstPreferences.Get().LanguageCode);
        Assert.Equal("en", secondPreferences.Get().LanguageCode);
        Assert.Equal(350, firstPreferences.Get().DailyLimitMinutes);
        Assert.Equal(95, firstPreferences.Get().PlanLimitMinutes);
        firstPreferences.Save(400, 120);
        Assert.Equal("uk", firstPreferences.Get().LanguageCode);
        Assert.Throws<ArgumentException>(() => firstPreferences.SaveLanguage("de"));
        Assert.Equal("uk", firstPreferences.Get().LanguageCode);
    }

    [Fact]
    public void ExistingWorkspaceMigratesWithoutLosingPreferencesOrTasks()
    {
        var user = new UserAccount { NormalizedEmail = "EXISTING@TEST.LOCAL" };
        using (var context = CreateContext())
        {
            context.GetService<IMigrator>().Migrate("20261118000300_AddOfflineGroups");
            context.Users.Add(user);
            context.Tasks.Add(new TaskItem("Особисте завдання"));
            context.SaveChanges();
            context.Database.ExecuteSqlInterpolated($"INSERT INTO UserPreferences (UserId, DailyLimitMinutes, PlanLimitMinutes) VALUES ({user.Id}, 420, 80)");
        }
        using (var context = CreateContext())
        {
            DatabaseInitializer.Initialize(context);
            Assert.False(context.Database.HasPendingModelChanges());
            var preferences = context.UserPreferences.Single();
            Assert.Equal("ru", preferences.LanguageCode);
            Assert.Equal(420, preferences.DailyLimitMinutes);
            Assert.Equal(80, preferences.PlanLimitMinutes);
            Assert.Equal("Особисте завдання", context.Tasks.Single().Title);
        }
        new UserPreferencesRepository(user.Id, CreateContext).SaveLanguage("en");
        using var restored = CreateContext();
        Assert.Equal("en", restored.UserPreferences.Single().LanguageCode);
    }

    [Fact]
    public void TranslationKeepsOptionIdentityAndDoesNotTranslateUserData()
    {
        LocalizationService.Current.ChangeLanguage("ru");
        var option = new LocalizedOption("Priority.High");
        var category = new CategoryOption("Мій власний текст");
        var task = new TaskItem("Название моей задачи");
        var changes = new List<string?>();
        option.PropertyChanged += (_, change) => changes.Add(change.PropertyName);
        LocalizationService.Current.ChangeLanguage("en");
        Assert.Equal("High", option.Label);
        Assert.NotEmpty(changes);
        Assert.Equal("Мій власний текст", category.Name);
        Assert.Equal("Название моей задачи", new TaskCard(task).Title);
        Assert.Equal("New", new TaskCard(task).Status);
        Assert.Equal("No deadline", new TaskCard(task).DueDate);
        Assert.Equal("Only the assigner can start a new task.", ServerErrors.Translate("Начать новую задачу может только постановщик."));
    }

    [Fact]
    public void StatisticsGroupByCategoryIdEvenWhenAUserNameMatchesTheTranslatedLabel()
    {
        LocalizationService.Current.ChangeLanguage("en");
        var category = new Category("No category", "#123456");
        var categorized = new TaskItem("With a category");
        categorized.SetCategory(category);
        var uncategorized = new TaskItem("Without a category");
        List<TaskItem> tasks = [categorized, uncategorized];
        var statistics = new StatisticsService();
        var english = statistics.Calculate(tasks, [], DateTime.Today, DateTime.Today.AddDays(1), DateTime.Now);
        Assert.Equal(2, english.Categories.Count);
        Assert.All(english.Categories, row => Assert.Equal(1, row.TaskCount));
        LocalizationService.Current.ChangeLanguage("uk");
        var ukrainian = statistics.Calculate(tasks, [], DateTime.Today, DateTime.Today.AddDays(1), DateTime.Now);
        Assert.Equal(2, ukrainian.Categories.Count);
        Assert.Contains(ukrainian.Categories, row => row.Category == "No category");
        Assert.Contains(ukrainian.Categories, row => row.Category == "Без категорії");
    }

    public void Dispose()
    {
        LocalizationService.Current.ChangeLanguage(_previousLanguage);
    }
}
