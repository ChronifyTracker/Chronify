using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Chronify.ViewModels;

public record TaskChoice(Guid Id, string Title);
public record WorkSessionRow(
    Guid Id,
    string Task,
    string Date,
    int Minutes,
    string Note
);
public record PlanFactRow(
    string Task,
    int Planned,
    int Actual
)
{
    public string Difference => Planned == 0 ? Texts.Get("WorkTime.NoPlan") : Texts.Format("WorkTime.Difference", Actual - Planned);
}

public partial class WorkTimeViewModel(
    Guid userId,
    ITaskRepository tasks,
    WorkSessionRepository sessions
) : PageViewModel
{
    public ObservableCollection<TaskChoice> Tasks { get; } = [];
    public ObservableCollection<WorkSessionRow> Sessions { get; } = [];
    public ObservableCollection<PlanFactRow> Comparison { get; } = [];
    [ObservableProperty]
    public partial TaskChoice? SelectedTask { get; set; }
    [ObservableProperty]
    public partial WorkSessionRow? SelectedSession { get; set; }
    [ObservableProperty]
    public partial DateTime? SessionDate { get; set; } = DateTime.Today;
    [ObservableProperty]
    public partial decimal? Minutes { get; set; } = 25;
    [ObservableProperty]
    public partial string? Note { get; set; }
    public event Action? DataChanged;

    public override void RefreshLanguage()
    {
        base.RefreshLanguage();
        Refresh();
    }

    public void Refresh()
    {
        Run(() =>
        {
            var allTasks = tasks.GetAll();
            var allSessions = sessions.GetAll();
            Guid? selectedId = SelectedTask?.Id;
            Guid? selectedSessionId = SelectedSession?.Id;
            Tasks.Clear();
            Sessions.Clear();
            Comparison.Clear();
            foreach (var task in allTasks)
            {
                Tasks.Add(new TaskChoice(task.Id, task.Title));
            }
            SelectedTask = Tasks.FirstOrDefault(t => t.Id == selectedId) ?? Tasks.FirstOrDefault();
            var names = allTasks.ToDictionary(t => t.Id, t => t.Title);
            foreach (var session in allSessions)
            {
                Sessions.Add(new WorkSessionRow(
                    session.Id,
                    names.GetValueOrDefault(session.TaskId, Texts.Get("WorkTime.DeletedTask")),
                    Texts.Date(session.StartedAt),
                    session.Minutes,
                    session.Note
                ));
            }
            SelectedSession = Sessions.FirstOrDefault(session => session.Id == selectedSessionId);
            foreach (var task in allTasks)
            {
                Comparison.Add(new PlanFactRow(task.Title, task.EstimatedMinutes,
                            allSessions.Where(s => s.TaskId == task.Id).Sum(s => s.Minutes)));
            }
        });
    }

    [RelayCommand]
    private void AddSession()
    {
        if (Run(() =>
        {
            if (SelectedTask is null)
            {
                throw new ArgumentException(Texts.Get("Errors.SelectTask"));
            }
            if (SessionDate is null)
            {
                throw new ArgumentException(Texts.Get("Errors.WorkDate"));
            }
            if (Minutes is not decimal minutes || minutes != decimal.Truncate(minutes) || minutes is < 1 or > 1440)
            {
                throw new ArgumentException(Texts.Get("Errors.MinutesRange"));
            }
            sessions.Add(new WorkSession(
                userId,
                SelectedTask.Id,
                SessionDate.Value.Date,
                (int)minutes,
                Note
            ));
        }))
        {
            Note = "";
            Refresh();
            DataChanged?.Invoke();
        }
    }

    [RelayCommand]
    private void DeleteSession()
    {
        if (SelectedSession is null)
        {
            ErrorMessage = Texts.Get("Errors.SelectWorkSession");
            return;
        }
        if (Run(() => sessions.Delete(SelectedSession.Id)))
        {
            Refresh();
            DataChanged?.Invoke();
        }
    }
}
