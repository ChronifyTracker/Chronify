using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Chronify.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ITaskRepository _repository;

    [ObservableProperty]
    public partial string DateText { get; set; } = "";

    [ObservableProperty]
    public partial bool LoadFailed { get; set; }

    [ObservableProperty]
    public partial bool IsHomePage { get; set; } = true;

    public ObservableCollection<TaskCard> UrgentTasks { get; } = [];
    public ObservableCollection<TaskCard> NewTasks { get; } = [];
    public ObservableCollection<TaskCard> AllTasks { get; } = [];

    public MainWindowViewModel(ITaskRepository repository)
    {
        _repository = repository;
        Refresh();
    }

    public void Refresh()
    {
        DateText = DateTime.Now.ToString("dddd, d MMMM", CultureInfo.GetCultureInfo("ru-RU"));

        try
        {
            var tasks = _repository.GetAll(new TaskQuery
            {
                SortBy = TaskSortBy.CreatedAt,
                SortDescending = true
            });
            var urgent = tasks.Where(t => t.Status != TaskStatus.Completed &&
                (t.Priority >= TaskPriority.High || t.DueDate <= DateTime.Now.AddDays(1)))
                .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                .ThenByDescending(t => t.Priority);
            var newTasks = tasks.Where(t => t.Status == TaskStatus.New);

            Fill(UrgentTasks, urgent);
            Fill(NewTasks, newTasks);
            Fill(AllTasks, tasks);
            LoadFailed = false;
        }
        catch (Exception)
        {
            LoadFailed = true;
        }
    }

    [RelayCommand]
    private void ShowHome()
    {
        IsHomePage = true;
        Refresh();
    }

    [RelayCommand]
    private void ShowAll()
    {
        IsHomePage = false;
        Refresh();
    }

    private static void Fill(ObservableCollection<TaskCard> cards, IEnumerable<TaskItem> tasks)
    {
        cards.Clear();

        foreach (var task in tasks)
            cards.Add(new TaskCard(task));
    }
}
