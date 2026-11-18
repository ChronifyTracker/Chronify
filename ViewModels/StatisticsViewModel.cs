using System.Collections.ObjectModel;
using Chronify.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Chronify.ViewModels;

public partial class StatisticsViewModel(ITaskRepository tasks, WorkSessionRepository sessions) : PageViewModel
{
    public IReadOnlyList<string> Periods { get; } = ["День", "Неделя", "Месяц"];
    public ObservableCollection<CategoryStatistics> Categories { get; } = [];
    [ObservableProperty]
    public partial int PeriodIndex { get; set; } = 2;
    [ObservableProperty]
    public partial DateTime? SelectedDate { get; set; } = DateTime.Today;
    [ObservableProperty]
    public partial string PeriodLabel { get; set; } = "";
    [ObservableProperty]
    public partial int Active { get; set; }
    [ObservableProperty]
    public partial int Overdue { get; set; }
    [ObservableProperty]
    public partial int Completed { get; set; }
    [ObservableProperty]
    public partial string OnTimeRate { get; set; } = "Нет данных";

    partial void OnPeriodIndexChanged(int value)
    {
        Refresh();
    }

    partial void OnSelectedDateChanged(DateTime? value)
    {
        Refresh();
    }

    public void Refresh()
    {
        Run(() =>
        {
            DateTime date = (SelectedDate ?? DateTime.Today).Date;
            DateTime from = PeriodIndex switch
            {
                1 => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
                2 => new DateTime(date.Year, date.Month, 1),
                _ => date
            };
            DateTime to = PeriodIndex == 2 ? from.AddMonths(1) : from.AddDays(PeriodIndex == 1 ? 7 : 1);
            var result = new StatisticsService().Calculate(
                tasks.GetAll(),
                sessions.GetAll(),
                from,
                to,
                DateTime.Now
            );
            Active = result.Active;
            Overdue = result.Overdue;
            Completed = result.Completed;
            OnTimeRate = result.OnTimeRate;
            PeriodLabel = $"{from:dd.MM.yyyy} — {to.AddDays(-1):dd.MM.yyyy}";
            Categories.Clear();
            foreach (var row in result.Categories)
            {
                Categories.Add(row);
            }
        });
    }
}
