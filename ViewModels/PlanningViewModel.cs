using System.Collections.ObjectModel;
using Chronify.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Chronify.ViewModels;

public record CalendarDay(
    string Number,
    string Load,
    string Background
);

public partial class PlanningViewModel(ITaskRepository tasks, UserPreferencesRepository preferences) : PageViewModel
{
    private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    public ObservableCollection<TaskCard> FocusTasks { get; } = [];
    public ObservableCollection<CalendarDay> Calendar { get; } = [];
    public IReadOnlyList<string> Weekdays { get; } =

    [

        "Пн",

        "Вт",

        "Ср",

        "Чт",

        "Пт",

        "Сб",

        "Вс"

    ];
    [ObservableProperty]
    public partial decimal? AvailableMinutes { get; set; } = 180;
    [ObservableProperty]
    public partial decimal? DailyLimit { get; set; } = 480;
    [ObservableProperty]
    public partial string PlanSummary { get; set; } = "Задайте время и сформируйте план.";
    public string MonthTitle => _month.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));

    public void Refresh()
    {
        Run(() =>
    {
        var saved = preferences.Get();
        AvailableMinutes = saved.PlanLimitMinutes;
        DailyLimit = saved.DailyLimitMinutes;
        LoadCalendar(saved.DailyLimitMinutes);
        if (FocusTasks.Count > 0)
        {
            BuildPlan();
        }
    });
    }

    [RelayCommand]
    private void BuildPlan()
    {
        Run(() =>
        {
            int limit = ReadMinutes(AvailableMinutes);
            int daily = ReadMinutes(DailyLimit);
            var result = new DayPlanningService().Build(tasks.GetAll(), limit, DateTime.Now);
            preferences.Save(daily, limit);
            FocusTasks.Clear();
            foreach (var task in result.Tasks)
            {
                FocusTasks.Add(new TaskCard(task));
            }
            PlanSummary = $"В плане: {result.Tasks.Count} задач, {result.UsedMinutes} из {limit} мин. Без оценки времени: {result.WithoutEstimate}.";
            LoadCalendar(daily);
        });
    }

    [RelayCommand]
    private void SaveLimits()
    {
        Run(() =>
    {
        preferences.Save(ReadMinutes(DailyLimit), ReadMinutes(AvailableMinutes));
        LoadCalendar(ReadMinutes(DailyLimit));
    });
    }

    [RelayCommand]
    private void PreviousMonth()
    {
        _month = _month.AddMonths(-1);
        UpdateMonth();
    }
    [RelayCommand]
    private void NextMonth()
    {
        _month = _month.AddMonths(1);
        UpdateMonth();
    }
    private void UpdateMonth()
    {
        OnPropertyChanged(nameof(MonthTitle));
        Run(() => LoadCalendar(ReadMinutes(DailyLimit)));
    }

    private void LoadCalendar(int limit)
    {
        Calendar.Clear();
        int offset = ((int)_month.DayOfWeek + 6) % 7;
        for (int i = 0; i < offset; i++)
        {
            Calendar.Add(new CalendarDay("", "", "Transparent"));
        }

        foreach (var day in new WorkloadService().BuildMonth(tasks.GetAll(), _month, limit))
        {
            Calendar.Add(new CalendarDay(day.Date.Day.ToString(), $"{day.Minutes} мин · {day.TaskCount} задач",
                        day.IsOverloaded ? "#542733" : day.TaskCount > 0 ? "#203D35" : "#1C191F"));
        }
    }

    private static int ReadMinutes(decimal? value)
    {
        if (value is not decimal minutes || minutes != decimal.Truncate(minutes) || minutes is < 1 or > 1440)
        {
            throw new ArgumentException("Укажите целое число минут от 1 до 1440.");
        }

        return (int)minutes;
    }
}
