using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Chronify;
using Chronify.Services;
using Chronify.ViewModels;
using Chronify.Views;
using Chronify.Services.Imports;
using Chronify.Teams;
using Chronify.Localization;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Avalonia.Interactivity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Chronify.TaskStatus;

if (args.Contains("--migrate-current"))
{
    using var connection = new SqliteConnection("Data Source=chronify.db");
    connection.Open();
    var taskQuery = SelectExistingColumns(connection, "Tasks");
    var categoryQuery = SelectExistingColumns(connection, "Categories");
    var tasksBefore = StoredRows(connection, taskQuery);
    var categoriesBefore = StoredRows(connection, categoryQuery);
    AppDbContext.InitializeDatabase();
    if (!tasksBefore.SequenceEqual(StoredRows(connection, taskQuery))
        || !categoriesBefore.SequenceEqual(StoredRows(
        connection,
        categoryQuery
    )))
    {
        throw new InvalidOperationException("Migration changed existing records");
    }

    Console.WriteLine($"Migrated local DB: {tasksBefore.Count} tasks and {categoriesBefore.Count} categories preserved.");
    return;
}

int checks = 0;
string directory = Path.Combine(Path.GetTempPath(), "ChronifyChecks", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
string database = Path.Combine(directory, "checks.db");
Func<AppDbContext> contextFactory = () => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={database}")
    .Options);
var tokens = new MemoryTokenStore();
var accounts = new FakeAccounts();
var auth = new AuthenticationService(tokens, contextFactory, accounts);
UserAccount first = null!;
UserAccount second = null!;
SqliteTaskRepository tasks = null!;
SqliteCategoryRepository categories = null!;
WorkSessionRepository sessions = null!;
FilterPresetRepository presets = null!;
UserPreferencesRepository preferences = null!;

try
{
    CheckMigration();
    CheckAuthentication();
    if (args.Contains("--localization-only"))
    {
        CheckOwnershipAndCrud();
        CheckAvaloniaLanguagesOnly();
        Console.WriteLine($"PASS: {checks} checks. Fixtures: {directory}");
        return;
    }
    CheckUnifiedAuthentication();
    CheckOwnershipAndCrud();
    CheckAlgorithms();
    CheckPages();
    CheckImports();
    if (args.Contains("--neon"))
    {
        checks += NeonChecks.Run(first, second, contextFactory).GetAwaiter().GetResult();
    }

    CheckAvalonia();
    Console.WriteLine($"PASS: {checks} checks. Fixtures: {directory}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FAIL after {checks} checks: {ex}");
    Environment.ExitCode = 1;
}
finally
{
    SqliteConnection.ClearAllPools();
}

void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }

    checks++;
}

void Throws(Action action, string message)
{
    bool thrown = false;
    try
    {
        action();
    }
    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or ModelValidationException or DbUpdateException)
    {
        thrown = true;
    }
    Check(thrown, message);
}

void CheckMigration()
{
    using (var connection = new SqliteConnection($"Data Source={database}"))
    {
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Categories (Id TEXT NOT NULL PRIMARY KEY, Name TEXT NOT NULL, Color TEXT NOT NULL);
            CREATE TABLE Tasks (Id TEXT NOT NULL PRIMARY KEY, Title TEXT NOT NULL, Description TEXT NOT NULL,
                DueDate TEXT NOT NULL, EstimatedMinutes INTEGER NOT NULL, Priority INTEGER NOT NULL,
                Status INTEGER NOT NULL, CreatedAt TEXT NOT NULL, CategoryId TEXT NULL,
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id));
            INSERT INTO Categories VALUES ('11111111-1111-1111-1111-111111111111','Legacy category','#123456');
            INSERT INTO Tasks VALUES ('22222222-2222-2222-2222-222222222222','Legacy task','Description',
                '2020-01-01 12:00:00',30,3,0,'2019-01-01 10:00:00','11111111-1111-1111-1111-111111111111');
            """;
        command.ExecuteNonQuery();
    }
    using (var context = contextFactory())
    {
        DatabaseInitializer.Initialize(context);
        Check(!context.Database.HasPendingModelChanges(), "Snapshot must match the model");
        Check(context.Database.GetAppliedMigrations().Count() == 5, "All five migrations must be applied");
        var task = context.Tasks.Include(t => t.Category).Single();
        Check(task is { Title: "Legacy task", UserId: null, CompletedAt: null, EstimatedMinutes: 30 }
            && task.Category?.Name == "Legacy category", "Legacy tasks and categories must be preserved");
        Check(
            task.IsOverdue && task.DueDate == new DateTime(
            2020,
            1,
            1,
            12,
            0,
            0
        ),
            "Legacy deadlines must be preserved"
        );
        Check(
            Directory.GetFiles(directory, "*.before-migration-*.db").Length == 1,
            "An upgrade backup is required"
        );
        DatabaseInitializer.Initialize(context);
        Check(
            Directory.GetFiles(directory, "*.before-migration-*.db").Length == 1,
            "Restart must not repeat the migration"
        );
    }
    using var fresh = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={Path.Combine(
        directory,
        "fresh.db"
    )}")
        .Options);
    DatabaseInitializer.Initialize(fresh);
    Check(!fresh.Database.GetPendingMigrations().Any(), "Fresh database must be supported");
}

void CheckAuthentication()
{
    Throws(
        () => auth.Register(
        "Student",
        "invalid",
        "Password-For-Checks!",
        false
    ),
        "Invalid email must be rejected"
    );
    Throws(
        () => auth.Register(
        "Student",
        "student@example.test",
        "short",
        false
    ),
        "Short password must be rejected"
    );
    first = auth.Register(
        "Student",
        "student@example.test",
        "Password-For-Checks!",
        true
    );
    Check(new TeamService(first, contextFactory).Connection?.RemoteUserId == first.Id,
        "App registration must immediately authorize groups");
    tasks = new SqliteTaskRepository(first.Id, contextFactory);
    categories = new SqliteCategoryRepository(first.Id, contextFactory);
    sessions = new WorkSessionRepository(first.Id, contextFactory);
    presets = new FilterPresetRepository(first.Id, contextFactory);
    preferences = new UserPreferencesRepository(first.Id, contextFactory);
    Check(tasks.GetAll().Count == 1 && categories.GetAll().Count == 1, "First account must inherit legacy data");
    Check(
        first.PasswordHash != "Password-For-Checks!" && first.PasswordSalt.Length > 0,
        "Password must be hashed with a salt"
    );
    Throws(
        () => auth.Register(
        "Duplicate",
        "STUDENT@example.test",
        "Password-For-Checks!",
        false
    ),
        "Emails must be unique ignoring case"
    );
    Throws(() => auth.Login(first.Email, "Wrong-Password!", false), "Wrong password must be rejected");
    var restored = new AuthenticationService(tokens, contextFactory, accounts);
    Check(restored.RestoreSession()?.Id == first.Id, "Remembered session must restore the correct user");
    restored.Logout();
    Check(tokens.Token is null && restored.RestoreSession() is null, "Logout must revoke and remove the token");
    second = auth.Register(
        "Another",
        "another@example.test",
        "Password-For-Checks!",
        false
    );
    Check(
        new SqliteTaskRepository(second.Id, contextFactory).GetAll().Count == 0,
        "Second account must not inherit legacy data"
    );
    Check(
        second.PasswordHash != first.PasswordHash && second.PasswordSalt != first.PasswordSalt,
        "Equal passwords need different salts and hashes"
    );
    auth.Login(first.Email, "Password-For-Checks!", false);
    Check(tokens.Token is null, "Non-remembered login must not persist a token");
    var unavailableStoreAuth = new AuthenticationService(new UnavailableTokenStore(), contextFactory, accounts);
    Check(unavailableStoreAuth.Login(first.Email, "Password-For-Checks!", true).Id == first.Id
        && unavailableStoreAuth.SessionNotice is not null, "Unavailable DPAPI must not prevent login");
    unavailableStoreAuth.Logout();
    auth.UpdateName(first, "Renamed student");
    using (var context = contextFactory())
    {
        Check(
            context.Users.Single(u => u.Id == first.Id).DisplayName == "Renamed student",
            "Profile change must persist"
        );
    }

    if (OperatingSystem.IsWindows())
    {
        string path = Path.Combine(directory, "remember.session.bin");
        var protectedStore = new SessionTokenStore(path);
        protectedStore.Write("Random-Test-Session-Token");
        Check(protectedStore.Read() == "Random-Test-Session-Token", "DPAPI token must round-trip");
        Check(
            !Encoding.UTF8.GetString(File.ReadAllBytes(path)).Contains("Random-Test-Session-Token"),
            "Session file must not contain plain token text"
        );
        protectedStore.Clear();
        Check(!File.Exists(path), "Clearing the remembered session must remove its file");
    }
}

void CheckUnifiedAuthentication()
{
    auth.Login(first.Email, "Password-For-Checks!", false);
    Check(accounts.Id(first.Email) == first.Id, "Registration must create the same identity remotely and locally");
    Check(
        new TeamService(first, contextFactory).Connection?.RemoteUserId == first.Id,
        "App login must immediately enable groups"
    );
    int remoteCount = accounts.Count;
    accounts.RejectRegistration = true;
    Throws(
        () => auth.Register(
        "Rejected",
        "rejected@example.test",
        "Password-For-Checks!",
        false
    ),
        "Remote registration refusal must be reported"
    );
    accounts.RejectRegistration = false;
    using (var db = contextFactory())
    {
        Check(!db.Users.Any(u => u.Email == "rejected@example.test") && accounts.Count == remoteCount,
        "Rejected registration must not create a local-only account");
    }

    accounts.Offline = true;
    Throws(
        () => auth.Register(
        "Offline",
        "offline@example.test",
        "Password-For-Checks!",
        false
    ),
        "Registration requires network for both databases"
    );
    Check(auth.Login(first.Email, "Password-For-Checks!", false).Id == first.Id
        && auth.SessionNotice!.Contains("без интернета"),
        "A cached account must still work offline");
    Throws(() => auth.Login(first.Email, "Wrong-Password!", false), "Offline login must check local password");
    Throws(
        () => auth.Login("unknown@example.test", "Password-For-Checks!", false),
        "First login on a computer requires network"
    );
    accounts.Offline = false;
    string cachePath = Path.Combine(directory, "second-computer.db");
    Func<AppDbContext> anotherComputer = () => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={cachePath}")
        .Options);
    using (var db = anotherComputer())
    {
        DatabaseInitializer.Initialize(db);
    }

    var otherLogin = new AuthenticationService(new MemoryTokenStore(), anotherComputer, accounts);
    var cached = otherLogin.Login(first.Email, "Password-For-Checks!", false);
    using (var db = anotherComputer())
    {
        Check(db.Users.Single().Id == first.Id && cached.Id == first.Id,
        "Ordinary login on another computer must create local cache without a second registration");
    }

    var globalToken = new ConnectionStore(cached.Id, anotherComputer).Secret("ChronifyNeon");
    otherLogin.Logout();
    Check(!accounts.IsSessionValid(globalToken) && new TeamService(cached, anotherComputer).Connection is null,
        "Logout must clear local group access and revoke the shared session");
    Check(new ConnectionStore(cached.Id, anotherComputer).Find("ChronifyNeon")!.RemoteUserId == cached.Id,
        "Logout must retain identity binding while clearing its token");
    const string legacyEmail = "legacy-local@example.test";
    var hash = PasswordHasher.Create("Password-For-Checks!");
    var legacy = new UserAccount
    {
        Email = legacyEmail,
        NormalizedEmail = legacyEmail.ToUpperInvariant(),
        DisplayName = "Legacy",
        PasswordHash = hash.Hash,
        PasswordSalt = hash.Salt
    };
    using (var db = contextFactory())
    {
        db.Users.Add(legacy); db.SaveChanges();
    }
    var legacyTask = new TaskItem("Preserved legacy personal task");
    new SqliteTaskRepository(legacy.Id, contextFactory).Add(legacyTask);
    var migrated = auth.Login(legacyEmail, "Password-For-Checks!", false);
    Check(migrated.Id == legacy.Id && accounts.Id(legacyEmail) is not null
        && new SqliteTaskRepository(
        legacy.Id,
        contextFactory
    ).GetById(legacyTask.Id) is not null,
        "Ordinary login must migrate legacy account while preserving local IDs and tasks");
    var duplicateGlobal = accounts.Register(new CloudCredentials(
        "same-email@example.test",
        "Global-Password!",
        "Global"
    ))
        .GetAwaiter()
        .GetResult();
    var oldHash = PasswordHasher.Create("Local-Password!");
    var oldLocal = new UserAccount
    {
        Email = "same-email@example.test",
        NormalizedEmail = "SAME-EMAIL@EXAMPLE.TEST",
        DisplayName = "Private local",
        PasswordHash = oldHash.Hash,
        PasswordSalt = oldHash.Salt
    };
    using (var db = contextFactory())
    {
        db.Users.Add(oldLocal); db.SaveChanges();
    }
    Throws(
        () => auth.Login(oldLocal.Email, "Global-Password!", false),
        "Matching email alone must not grant access to old local data"
    );
    using (var db = contextFactory())
    {
        Check(db.ServiceConnections.Find(oldLocal.Id, "ChronifyNeon") is null,
        "Unverified legacy identity must remain unbound");
    }

    auth.Login(first.Email, "Password-For-Checks!", false);
}

void CheckOwnershipAndCrud()
{
    var otherTasks = new SqliteTaskRepository(second.Id, contextFactory);
    var otherCategories = new SqliteCategoryRepository(second.Id, contextFactory);
    var category = new Category("Algorithms", "#81C784");
    categories.Add(category);
    Throws(() => categories.Add(new Category("ALGORITHMS", "#123456")), "Category names must be unique for a user");
    otherCategories.Add(new Category("Algorithms", "#123456"));
    Check(otherCategories.GetAll().Count == 1, "Different users may use equal category names");
    var task = new TaskItem(
        "Owned task",
        dueDate: DateTime.Today.AddDays(1),
        estimatedMinutes: 60,
        category: category
    );
    tasks.Add(task);
    Check(tasks.GetById(task.Id)?.CategoryId == category.Id, "Existing category must attach without duplication");
    Check(
        otherTasks.GetById(task.Id) is null && !otherTasks.Delete(task.Id) && !otherTasks.Update(task),
        "Other account must not read, update or delete the task"
    );
    Check(
        otherCategories.GetById(category.Id) is null && !otherCategories.Delete(category.Id),
        "Category ownership must be checked"
    );
    Throws(
        () => otherTasks.Add(new TaskItem("Cross-account category", category: category)),
        "Cross-account category assignment must be rejected"
    );
    task.StartTask(); tasks.Update(task);
    Check(tasks.GetById(task.Id)?.Status == TaskStatus.InProgress, "Start must persist");
    task.CompleteTask(); tasks.Update(task);
    Check(tasks.GetById(task.Id)?.CompletedAt is not null, "Completion timestamp must persist");
    Throws(() => task.SetTitle("Closed edit"), "Completed task must refuse editing");
    task.ReopenTask(); tasks.Update(task);
    Check(tasks.GetById(task.Id)?.CompletedAt is null, "Reopen must clear completion timestamp");
    var copy = task.Copy(); copy.SetTitle("Uncommitted edit");
    Check(task.Title == "Owned task", "Editing a copy must not mutate the original");
    var session = new WorkSession(
        first.Id,
        task.Id,
        DateTime.Today,
        45,
        "Practice"
    );
    sessions.Add(session);
    Check(sessions.GetAll().Single().Minutes == 45, "Work session must persist");
    Check(
        new WorkSessionRepository(second.Id, contextFactory).GetAll().Count == 0,
        "Work sessions must be user-scoped"
    );
    Throws(
        () => new WorkSessionRepository(second.Id, contextFactory).Add(session),
        "Cross-account work session must be rejected"
    );
    Throws(
        () => new WorkSession(
        first.Id,
        task.Id,
        DateTime.Now.AddDays(1),
        10,
        ""
    ),
        "Future work session must be rejected"
    );
    Throws(() => new WorkSession(
        first.Id,
        task.Id,
        DateTime.Today,
        0,
        ""
    ), "Zero-minute session must be rejected");
    Throws(() => new TaskItem("Past", dueDate: DateTime.Today.AddDays(-1)), "Past deadline must be rejected");
    Throws(() => new TaskItem("Invalid priority", priority: (TaskPriority)99), "Invalid priority must be rejected");
    var legacy = tasks.GetById(Guid.Parse("22222222-2222-2222-2222-222222222222"))!;
    var edit = new AddTaskViewModel(tasks, categories, legacy) { Title = "Edited legacy" };
    Check(
        edit.Save() && tasks.GetById(legacy.Id)?.Title == "Edited legacy",
        "Unchanged overdue deadline may be preserved in an edit"
    );
    categories.Delete(category.Id);
    Check(tasks.GetById(task.Id)?.CategoryId is null, "Deleting category must preserve tasks");
    tasks.Delete(task.Id);
    Check(sessions.GetAll().Count == 0, "Deleting task must remove its work sessions");
    var noDeadline = new TaskItem("No deadline"); tasks.Add(noDeadline);
    Check(tasks.GetById(noDeadline.Id)?.DueDate is null, "No-deadline tasks must persist");
    for (int i = 0; i < 5; i++)
    {
        presets.Save(
            "Preset " + i,
            new TaskQuery { Search = "abc", DateFilter = TaskDateFilter.Overdue, SortBy = TaskSortBy.Edf }
        );
    }

    Throws(() => presets.Save("Sixth", new TaskQuery()), "Only five filter presets are allowed");
    presets.Save("Preset 0", new TaskQuery { Search = "updated" });
    Check(
        presets.GetAll().Count == 5 && presets.GetAll().First().Search == "updated",
        "Existing preset must be replaceable"
    );
    Check(new FilterPresetRepository(second.Id, contextFactory).GetAll().Count == 0, "Presets must be private");
    Throws(() => new FilterPresetRepository(second.Id, contextFactory).Save("Foreign category",
        new TaskQuery { CategoryId = categories.GetAll().First().Id }), "Preset must not reference another user's category");
    foreach (var preset in presets.GetAll())
    {
        presets.Delete(preset.Id);
    }

    preferences.Save(300, 120);
    Check(preferences.Get() is { DailyLimitMinutes: 300, PlanLimitMinutes: 120 }, "Time limits must persist");
    Throws(() => preferences.Save(0, 120), "Invalid time limit must be rejected");
}

void CheckAlgorithms()
{
    var now = DateTime.Now;
    var soon = new TaskItem(
        "Soon",
        dueDate: now.AddHours(2),
        estimatedMinutes: 40,
        priority: TaskPriority.High
    );
    var later = new TaskItem(
        "Later",
        dueDate: now.AddDays(5),
        estimatedMinutes: 60,
        priority: TaskPriority.Low
    );
    var unknown = new TaskItem("Unknown estimate");
    var closed = new TaskItem(
        "Closed",
        dueDate: now.AddDays(1),
        estimatedMinutes: 30,
        status: TaskStatus.Completed
    );
    var list = new List<TaskItem> { later, unknown, closed, soon };
    Check(
        TaskFilteringService.Apply(list, new TaskQuery { Search = "SOON" }, now).Single().Id == soon.Id,
        "Search must ignore case"
    );
    Check(
        TaskFilteringService.Apply(list, new TaskQuery { DateFilter = TaskDateFilter.Today }, now).All(t => t.DueDate?.Date == now.Date),
        "Today filter must respect calendar dates"
    );
    Check(TaskFilteringService.Apply(
        list,
        new TaskQuery { DateFilter = TaskDateFilter.NextThreeDays, Status = TaskStatus.New },
        now
    )
        .Single()
        .Id == soon.Id,
        "Filters must combine");
    Check(
        TaskFilteringService.Apply(list, new TaskQuery { SortBy = TaskSortBy.Edf }, now).First().Id == soon.Id,
        "EDF must put earliest deadline first"
    );
    Check(
        DynamicPriorityCalculator.Calculate(soon, now) > DynamicPriorityCalculator.Calculate(later, now),
        "Urgency and importance must affect the score"
    );
    Check(DynamicPriorityCalculator.Calculate(closed, now) == 0, "Completed task has no planning priority");
    var plan = new DayPlanningService().Build(list, 70, now);
    Check(
        plan.UsedMinutes <= 70 && plan.Tasks.Single().Id == soon.Id && plan.WithoutEstimate == 1,
        "Plan must fit limit and skip unknown estimates"
    );
    var workload = new WorkloadService().BuildMonth(new[] { soon, soon, closed }, now, 60);
    Check(workload.Single(d => d.Date == soon.DueDate!.Value.Date).IsOverloaded, "Daily overload must be detected");
    Check(workload.Count == DateTime.DaysInMonth(now.Year, now.Month), "Calendar must contain each day");
    var stats = new StatisticsService().Calculate(
        list,
        [],
        now.Date,
        now.Date.AddDays(7),
        now
    );
    Check(
        stats.Completed == 1 && stats.OnTimeRate == "100%",
        "On-time rate must compare real completion with deadline"
    );
    var empty = new StatisticsService().Calculate(
        [],
        [],
        now.Date,
        now.Date.AddDays(1),
        now
    );
    Check(empty.OnTimeRate == "Нет данных", "Empty on-time denominator must not show a misleading percentage");
    using var context = contextFactory();
    var externalTask = new TaskItem("Future import"); tasks.Add(externalTask);
    context.ExternalSources.Add(new ExternalSourceInfo { UserId = first.Id, TaskId = externalTask.Id, Provider = "Classroom", ExternalId = "external-1", Url = "https://example.test/task" });
    context.SaveChanges();
    Check(
        context.ExternalSources.Count() == 1,
        "External source metadata must persist independently of an HTTP client"
    );
    var anotherTask = new TaskItem("Another import"); tasks.Add(anotherTask);
    context.ExternalSources.Add(new ExternalSourceInfo { UserId = first.Id, TaskId = anotherTask.Id, Provider = "Classroom", ExternalId = "external-1" });
    Throws(() => context.SaveChanges(), "Repeated external source IDs must be unique per user and provider");
}

MainWindowViewModel CreateMain()
{
    return new(
        first,
        tasks,
        categories,
        sessions,
        presets,
        preferences,
        auth,
        contextFactory
    );
}

void CheckPages()
{
    var main = CreateMain();
    Check(main.AllTasks.Count == tasks.GetAll().Count, "Main page must load the current user's tasks");
    main.SearchText = "No deadline";
    Check(main.AllTasks.Count == 1, "Search changes must immediately refresh visible tasks");
    main.PresetName = "My search"; main.SavePresetCommand.Execute(null);
    main.ResetFiltersCommand.Execute(null);
    main.SelectedPreset = main.Presets.Single(); main.ApplyPresetCommand.Execute(null);
    Check(
        main.SearchText == "No deadline" && main.AllTasks.Count == 1,
        "Applying saved preset must restore filters"
    );
    main.ResetFiltersCommand.Execute(null);
    var categoryPage = main.CategoriesPage;
    categoryPage.Name = "UI category"; categoryPage.Color = "#64B5F6"; categoryPage.SaveCommand.Execute(null);
    Check(categories.GetAll().Any(c => c.Name == "UI category"), "Category editor must add a category");
    categoryPage.SelectedCategory = categoryPage.Categories.Single(c => c.Name == "UI category");
    categoryPage.Name = "Renamed in UI"; categoryPage.SaveCommand.Execute(null);
    Check(categories.GetAll().Any(c => c.Name == "Renamed in UI"), "Category editor must update a category");
    main.ShowPlanningCommand.Execute(null);
    main.PlanningPage.AvailableMinutes = 90; main.PlanningPage.DailyLimit = 400; main.PlanningPage.BuildPlanCommand.Execute(null);
    Check(
        main.PlanningPage.ErrorMessage is null && main.PlanningPage.Calendar.Count > 27,
        "Planning page must generate a calendar and a plan"
    );
    main.ShowWorkTimeCommand.Execute(null);
    var time = main.WorkTimePage;
    time.SelectedTask = time.Tasks.Single(t => t.Title == "No deadline");
    time.Minutes = 25; time.AddSessionCommand.Execute(null);
    Check(
        time.Sessions.Count == 1 && time.Comparison.Single(t => t.Task == "No deadline").Actual == 25,
        "Time entry must update plan/fact comparison"
    );
    main.ShowStatisticsCommand.Execute(null);
    Check(
        main.StatisticsPage.Categories.Sum(c => c.ActualMinutes) == 25,
        "Statistics must include recorded work sessions"
    );
}

void CheckImports()
{
    using var notion = JsonDocument.Parse("""
        {"id":"notion-check-page","url":"https://www.notion.so/check","properties":{
        "Name":{"type":"title","title":[{"plain_text":"Notion title"}]},
        "Deadline":{"type":"date","date":{"start":"2020-01-02"}}}}
        """);
    var parsed = NotionProvider.Parse(new("table", "Tasks"), notion.RootElement);
    Check(
        parsed.Title == "Notion title" && parsed.DueDate == new DateTime(
        2020,
        1,
        2,
        23,
        59,
        59
    ),
        "Notion title and all-day deadline must be parsed"
    );
    using var classroom = JsonDocument.Parse("""
        {"id":"assignment","title":"Classroom title","description":"Offline instructions","dueDate":{"year":2020,"month":1,"day":2},"dueTime":{"hours":12,"minutes":30}}
        """);
    var google = ClassroomProvider.Parse(new("course", "Algorithms"), classroom.RootElement);
    Check(
        google.Id == "course:assignment"
        && google.DueDate?.ToUniversalTime() == new DateTime(
        2020,
        1,
        2,
        12,
        30,
        0,
        DateTimeKind.Utc
    ),
        "Classroom UTC deadlines and course-scoped IDs must be parsed"
    );
    var provider = new FixtureProvider();
    parsed.Attachments.Add(new ExternalAttachment(
        "conditions.txt",
        Encoding.UTF8.GetBytes("Offline file contents")
    ));
    var importer = new TaskImportService(first.Id, contextFactory);
    Check(importer.Import(provider, new[] { parsed, google, parsed }, CancellationToken.None).GetAwaiter()
        .GetResult() == 2,
        "Import must accept overdue tasks and deduplicate selected IDs");
    Check(importer.Import(provider, new[] { parsed, google }, CancellationToken.None).GetAwaiter().GetResult() == 0,
        "Repeated import must not overwrite local tasks");
    var imported = tasks.GetAll().Single(t => t.Title == "Notion title");
    using (var storage = contextFactory())
    {
        Check(Encoding.UTF8.GetString(storage.TaskAttachments.Single(a => a.TaskId == imported.Id
            && a.UserId == first.Id)
            .Content) == "Offline file contents",
                "Imported attachments must be stored as bytes in SQLite");
    }

    Check(new TaskDetailsViewModel(imported, first.Id, contextFactory).Attachments.Count == 1,
            "Task details must list locally stored attachments");
    Check(
        imported.Description.Contains("Saved page contents") && imported.Category?.Name == "Tasks",
        "Notion body must be stored in SQLite as description"
    );
    Check(
        new SqliteTaskRepository(second.Id, contextFactory).GetById(imported.Id) is null,
        "Imports must be private to the current account"
    );
    using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
    bool cancelled = false;
    try
    {
        importer.Import(provider, new[] { parsed with { Id = "cancelled-page" } }, cancellation.Token).GetAwaiter()
              .GetResult();
    }
    catch (OperationCanceledException)
    {
        cancelled = true;
    }
    using var db = contextFactory();
    Check(
        cancelled && !db.ExternalSources.Any(s => s.ExternalId == "cancelled-page"),
        "Cancelled imports must not create partial tasks"
    );
}

void CheckAvalonia()
{
    AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
        .SetupWithoutStarting();
    var loginModel = new LoginViewModel(auth) { IsRegistration = true, RememberMe = false };
    UserAccount? signedIn = null;
    loginModel.SignedIn += user => signedIn = user;
    var login = new LoginWindow { DataContext = loginModel };
    login.Show(); Dispatcher.UIThread.RunJobs();
    Check(login.FindControl<TextBox>("EmailInput") is not null, "Login view must load");
    login.FindControl<TextBox>("NameInput")!.Text = "UI student";
    login.FindControl<TextBox>("EmailInput")!.Text = "ui.student@example.test";
    login.FindControl<TextBox>("PasswordInput")!.Text = "UI-Password-Checks!";
    login.FindControl<TextBox>("ConfirmPasswordInput")!.Text = "UI-Password-Checks!";
    Pump(loginModel.SubmitCommand.ExecuteAsync(null));
    Check(
        signedIn?.Email == "ui.student@example.test" && loginModel.Password == "",
        "Registration form must register and clear password inputs"
    );
    loginModel.IsRegistration = false;
    signedIn = null;
    login.FindControl<TextBox>("PasswordInput")!.Text = "UI-Password-Checks!";
    Pump(loginModel.SubmitCommand.ExecuteAsync(null));
    Check(
        signedIn is not null && loginModel.ErrorMessage is null,
        "Login form must authenticate an existing local account"
    );
    Capture(login, "login"); login.Close();
    auth.Login(first.Email, "Password-For-Checks!", false);
    var mainModel = CreateMain();
    var main = new MainWindow { DataContext = mainModel };
    main.Show(); Dispatcher.UIThread.RunJobs(); Capture(main, "home");
    mainModel.ShowGroupsCommand.Execute(null);
    Capture(main, "groups-unified-login");
    var groupsView = main.GetVisualDescendants().OfType<GroupsView>().Single();
    Check(groupsView.GetVisualDescendants().OfType<Button>().Any(b => Equals(b.Content, "Создать группу"))
        && groupsView.GetVisualDescendants().OfType<Button>().Any(b => Equals(b.Content, "Войти в группу"))
        && !groupsView.GetVisualDescendants().OfType<TextBox>().Any(t => t.PasswordChar != default),
        "Groups view must offer create/join actions and no second password form");
    mainModel.ShowHomeCommand.Execute(null);
    Dispatcher.UIThread.RunJobs();
    var details = new TaskDetailsWindow
    {
        DataContext = mainModel.TaskDetails(tasks.GetAll().Single(t => t.Title == "Notion title")
        .Id)
    };
    details.Show(main); Dispatcher.UIThread.RunJobs();
    Check(
        details.GetVisualDescendants().OfType<TextBox>().Single().Text!.Contains("Saved page contents"),
        "Details view must show full locally stored conditions"
    );
    Capture(details, "task-details"); details.Close();
    var add = main.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "+  Добавить задачу"));
    Click(main, add);
    var menu = (MenuFlyout)add.Flyout!;
    Check(!menu.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Выбрать из Classroom")).IsEnabled,
        "Classroom import must be disabled until connected");
    menu.Items.OfType<MenuItem>().Single(i => Equals(i.Header, "Вручную")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
    menu.Hide();
    Dispatcher.UIThread.RunJobs();
    var form = main.OwnedWindows.OfType<AddTaskWindow>().Single();
    form.FindControl<TextBox>("TitleInput")!.Text = "UI-created task";
    form.GetVisualDescendants().OfType<NumericUpDown>().Single().Value = 40;
    Click(form, form.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Добавить")));
    Check(
        tasks.GetAll().Any(t => t.Title == "UI-created task") && !main.OwnedWindows.Any(),
        "Task dialog must save through the real scoped repository"
    );
    var navigation = new (string Label, int Page, string Image)[]
    {
        ("Все задачи", 1, "tasks"), ("Категории", 2, "categories"), ("План и нагрузка", 3, "planning"),
        ("Статистика", 4, "statistics"), ("Учёт времени", 5, "work-time"), ("Личный кабинет", 6, "account"), ("Группы", 7, "groups")
    };
    foreach (var page in navigation)
    {
        var button = main.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, page.Label));
        Click(main, button);
        Check(
            mainModel.SelectedPage == page.Page && button.Classes.Contains("selected"),
            "Navigation failed: " + page.Label
        );
        if (page.Page == 3)
        {
            mainModel.PlanningPage.BuildPlanCommand.Execute(null);
        }

        Capture(main, page.Image);
    }
    mainModel.ShowAllCommand.Execute(null); Dispatcher.UIThread.RunJobs();
    var search = main.GetVisualDescendants().OfType<TextBox>().Single(t => t.Watermark?.ToString() == "Поиск по названию и описанию");
    search.Text = "UI-created"; Dispatcher.UIThread.RunJobs();
    Check(mainModel.AllTasks.Count == 1, "Search box binding must filter the DataGrid");
    Guid uiTaskId = tasks.GetAll().Single(t => t.Title == "UI-created task").Id;
    Button RowAction(string name)
    {
        main.CaptureRenderedFrame();
        Dispatcher.UIThread.RunJobs();
        return main.GetVisualDescendants().OfType<Button>().Single(b => b.DataContext is TaskCard card
            && card.Id == uiTaskId && b.GetValue(Avalonia.Automation.AutomationProperties.NameProperty) == name
            && b.IsEffectivelyVisible);
    }
    Click(main, RowAction("Начать"));
    Check(tasks.GetById(uiTaskId)?.Status == TaskStatus.InProgress, "Start button must still work");
    Click(main, RowAction("Завершить"));
    Check(
        tasks.GetById(uiTaskId)?.CompletedAt is not null && !RowAction("Изменить").IsEnabled,
        "Completion must lock task editing"
    );
    Click(main, RowAction("Вернуть в работу"));
    Check(tasks.GetById(uiTaskId)?.CompletedAt is null, "Reopen button must clear completion time");
    Click(main, RowAction("Изменить"));
    var editing = main.OwnedWindows.OfType<AddTaskWindow>().Single();
    editing.FindControl<TextBox>("TitleInput")!.Text = "UI-created edited task";
    Click(editing, editing.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Сохранить")));
    Check(
        tasks.GetById(uiTaskId)?.Title == "UI-created edited task",
        "Task edit dialog must persist the copied entity"
    );
    CheckLocalization(main, mainModel);
    bool logoutRequested = false;
    mainModel.LogoutRequested += () => logoutRequested = true;
    Pump(mainModel.AccountPage.LogoutCommand.ExecuteAsync(null));
    Check(
        logoutRequested && auth.RestoreSession() is null,
        "Account page must revoke the session and request the login screen"
    );
    main.Close();
}

void CheckLocalization(MainWindow main, MainWindowViewModel model)
{
    model.ShowAccountCommand.Execute(null);
    Dispatcher.UIThread.RunJobs();
    var selector = main.GetVisualDescendants().OfType<AccountView>().Single()
        .FindControl<ComboBox>("LanguageSelector")!;
    var formModel = model.CreateAddTask();
    var form = new AddTaskWindow { DataContext = formModel };
    form.Show(main);
    var login = new LoginWindow { DataContext = new LoginViewModel(auth) };
    login.Show();
    Dispatcher.UIThread.RunJobs();

    model.FilterStatusIndex = 2;
    model.FilterPriorityIndex = 3;
    model.SortIndex = 5;
    object statusOption = model.StatusFilters[2];
    model.AccountPage.DisplayName = "Unsaved profile name";
    model.GroupsPage.TaskTitle = "Чернетка задачі";
    model.WorkTimePage.Note = "Моя заметка";
    model.PlanningPage.AvailableMinutes = 135;
    formModel.Title = "Не переводить название";
    formModel.Description = "Keep this description";
    formModel.Minutes = 47;
    formModel.DueDate = DateTime.Today.AddDays(2);
    var taskData = tasks.GetAll().Select(task => (task.Id, task.Title, task.Description, task.CategoryId)).OrderBy(task => task.Id).ToList();

    foreach (string code in new[] { "uk", "en", "ru" })
    {
        selector.SelectedItem = model.AccountPage.Languages.Single(language => language.Code == code);
        Dispatcher.UIThread.RunJobs();
        Check(LocalizationService.Current.LanguageCode == code, "Language selector must update the localization service");
        Check(preferences.Get().LanguageCode == code, "Language selector must persist the choice in SQLite");
        Check(main.GetVisualDescendants().OfType<Button>().Any(button => Equals(button.Content, Texts.Get("Navigation.Home"))),
            "Existing navigation buttons must change language: " + code + "; actual: " + string.Join(", ", main.GetVisualDescendants().OfType<Button>().Where(button => button.Classes.Contains("nav")).Select(button => button.Content)));
        Check(model.PageTitle == Texts.Get("Navigation.Account"), "The current page heading must change language");
        Check(form.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == Texts.Get("Tasks.NewHeading")),
            "Already open task dialogs must change language");
        Check(form.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == Texts.Get("Priority.Medium")),
            "The selected priority label must change language without reopening its dropdown");
        Check(login.Title == Texts.Get("Login.WindowTitle"), "Already open login windows must change language");
        Check(main.GetVisualDescendants().OfType<DataGrid>().Any(grid => grid.Columns.Any(column => Equals(column.Header, Texts.Get("Tasks.Status")))),
            "DataGrid column headers must change language");
        Check(model.FilterStatusIndex == 2 && model.FilterPriorityIndex == 3 && model.SortIndex == 5
            && ReferenceEquals(statusOption, model.StatusFilters[2]), "Translation must preserve filter indices and option identity");
        Check(model.AccountPage.DisplayName == "Unsaved profile name" && model.GroupsPage.TaskTitle == "Чернетка задачі"
            && model.WorkTimePage.Note == "Моя заметка" && model.PlanningPage.AvailableMinutes == 135,
            "Translation must preserve unsaved values on every page");
        Check(formModel.Title == "Не переводить название" && formModel.Description == "Keep this description" && formModel.Minutes == 47,
            "Translation must preserve a task draft in an open dialog");
        Check(model.DateText == DateTime.Now.ToString("dddd, d MMMM", LocalizationService.Current.Culture),
            "The header date must use the selected culture");
        Check(System.Globalization.CultureInfo.CurrentCulture.Name == LocalizationService.Current.Culture.Name,
            "The UI thread culture must follow the language selector");
        Check(form.GetVisualDescendants().OfType<CalendarDatePicker>().Single().Text == Texts.Date(formModel.DueDate!.Value),
            "An already selected date must be reformatted without losing its value");
        Check(taskData.SequenceEqual(tasks.GetAll().Select(task => (task.Id, task.Title, task.Description, task.CategoryId)).OrderBy(task => task.Id)),
            "Switching language must not modify stored task data");
        Capture(main, "language-" + code);
        Capture(form, "task-language-" + code);
    }

    selector.SelectedItem = model.AccountPage.Languages.Single(language => language.Code == "en");
    Dispatcher.UIThread.RunJobs();
    LocalizationService.Current.ChangeLanguage("ru");
    var restored = CreateMain();
    Check(LocalizationService.Current.LanguageCode == "en" && restored.AccountPage.SelectedLanguage?.Code == "en",
        "Opening a workspace must restore its saved language");
    Check(new UserPreferencesRepository(second.Id, contextFactory).Get().LanguageCode == "ru",
        "Language preferences must be isolated between accounts");
    selector.SelectedItem = model.AccountPage.Languages.Single(language => language.Code == "ru");
    Dispatcher.UIThread.RunJobs();
    login.Close();
    form.Close();
}

void CheckAvaloniaLanguagesOnly()
{
    AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
        .SetupWithoutStarting();
    var model = CreateMain();
    var main = new MainWindow { DataContext = model };
    main.Show();
    Dispatcher.UIThread.RunJobs();
    CheckLocalization(main, model);
    main.Close();
}

static void Pump(Task pending)
{
    DateTime deadline = DateTime.UtcNow.AddSeconds(15);
    while (!pending.IsCompleted && DateTime.UtcNow < deadline)
    {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(5);
    }
    if (!pending.IsCompleted)
    {
        throw new TimeoutException("UI command timed out");
    }

    pending.GetAwaiter().GetResult();
    Dispatcher.UIThread.RunJobs();
}

void Capture(Window window, string name)
{
    Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);
    window.CaptureRenderedFrame()!.Save(Path.Combine(AppContext.BaseDirectory, "qa-" + name + ".png"));
}

static void Click(Window window, Button button)
{
    var point = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
    window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Dispatcher.UIThread.RunJobs();
}

static string SelectExistingColumns(SqliteConnection connection, string table)
{
    using var command = connection.CreateCommand(); command.CommandText = $"PRAGMA table_info(\"{table}\")";
    using var reader = command.ExecuteReader(); var columns = new List<string>();
    while (reader.Read())
    {
        columns.Add('"' + reader.GetString(1) + '"');
    }

    return $"SELECT {string.Join(',', columns)} FROM \"{table}\" ORDER BY Id";
}

static List<string> StoredRows(SqliteConnection connection, string sql)
{
    using var command = connection.CreateCommand(); command.CommandText = sql;
    using var reader = command.ExecuteReader(); var rows = new List<string>();
    while (reader.Read())
    {
        var row = new StringBuilder();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            string? value = reader.IsDBNull(i) ? null : reader.GetValue(i).ToString();
            row.Append(value?.Length ?? -1).Append(':').Append(value);
        }
        rows.Add(row.ToString());
    }
    return rows;
}

sealed class MemoryTokenStore : ISessionTokenStore
{
    public string? Token { get; private set; }
    public string? Read()
    {
        return Token;
    }

    public void Write(string token)
    {
        Token = token;
    }

    public void Clear()
    {
        Token = null;
    }
}

sealed class UnavailableTokenStore : ISessionTokenStore
{
    public string? Read()
    {
        return null;
    }

    public void Write(string token)
    {
        throw new System.Security.Cryptography.CryptographicException("Unavailable test profile");
    }

    public void Clear() { }
}

sealed class FixtureProvider : IExternalTaskProvider
{
    public Task<List<ExternalTaskSource>> Sources(CancellationToken cancellation)
    {
        return Task.FromResult(new List<ExternalTaskSource>());
    }

    public Task<List<ExternalTask>> Tasks(ExternalTaskSource source, CancellationToken cancellation)
    {
        return Task.FromResult(new List<ExternalTask>());
    }

    public Task<string> Description(ExternalTask task, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        return Task.FromResult(task.Description.Length > 0 ? task.Description : "Saved page contents");
    }
}
