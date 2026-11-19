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
    private DayPlan? _plan;
    private int _calendarLimit = 480;
    public ObservableCollection<TaskCard> FocusTasks { get; } = [];
    public ObservableCollection<CalendarDay> Calendar { get; } = [];
    public IReadOnlyList<string> Weekdays =>
    [
        Texts.Get("Weekdays.Monday"),
        Texts.Get("Weekdays.Tuesday"),
        Texts.Get("Weekdays.Wednesday"),
        Texts.Get("Weekdays.Thursday"),
        Texts.Get("Weekdays.Friday"),
        Texts.Get("Weekdays.Saturday"),
        Texts.Get("Weekdays.Sunday")
    ];
    [ObservableProperty]
    public partial decimal? AvailableMinutes { get; set; } = 180;
    [ObservableProperty]
    public partial decimal? DailyLimit { get; set; } = 480;
    public string PlanSummary => _plan is null
        ? Texts.Get("Planning.InitialSummary")
        : Texts.Format("Planning.Summary", _plan.Tasks.Count, _plan.UsedMinutes, _plan.LimitMinutes, _plan.WithoutEstimate);
    public string MonthTitle => _month.ToString("MMMM yyyy", LocalizationService.Current.Culture);

    public override void RefreshLanguage()
    {
        base.RefreshLanguage();
        if (_plan is not null)
        {
            FocusTasks.Clear();
            foreach (var task in _plan.Tasks)
            {
                FocusTasks.Add(new TaskCard(task));
            }
        }
        Run(() => LoadCalendar(_calendarLimit));
    }

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
            _plan = result;
            OnPropertyChanged(nameof(PlanSummary));
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
        _calendarLimit = limit;
        Calendar.Clear();
        int offset = ((int)_month.DayOfWeek + 6) % 7;
        for (int i = 0; i < offset; i++)
        {
            Calendar.Add(new CalendarDay("", "", "Transparent"));
        }

        foreach (var day in new WorkloadService().BuildMonth(tasks.GetAll(), _month, limit))
        {
            Calendar.Add(new CalendarDay(day.Date.Day.ToString(), Texts.Format("Planning.DayLoad", day.Minutes, day.TaskCount),
                        day.IsOverloaded ? "#542733" : day.TaskCount > 0 ? "#203D35" : "#1C191F"));
        }
    }

    private static int ReadMinutes(decimal? value)
    {
        if (value is not decimal minutes || minutes != decimal.Truncate(minutes) || minutes is < 1 or > 1440)
        {
            throw new ArgumentException(Texts.Get("Errors.MinutesRange"));
        }

        return (int)minutes;
    }
}
