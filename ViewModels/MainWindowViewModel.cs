using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Chronify.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ITaskRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMinutes(1) };

    [ObservableProperty]
    public partial string DateText { get; set; } = "";

    [ObservableProperty]
    public partial bool LoadFailed { get; set; }

    [ObservableProperty]
    public partial bool IsHomePage { get; set; } = true;

    public ObservableCollection<TaskCard> UrgentTasks { get; } = [];
    public ObservableCollection<TaskCard> NewTasks { get; } = [];
    public ObservableCollection<TaskCard> AllTasks { get; } = [];

    public MainWindowViewModel(ITaskRepository repository, ICategoryRepository categoryRepository)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        Refresh();

        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
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
                (t.Priority >= TaskPriority.High || t.IsOverdue || t.IsDueSoon))
                .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                .ThenByDescending(t => t.Priority);
            var newTasks = tasks.Where(t => t.Status == TaskStatus.New);

            Fill(UrgentTasks, urgent);
            Fill(NewTasks, newTasks);
            Fill(AllTasks, tasks);
            LoadFailed = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            LoadFailed = true;
        }
    }

    public AddTaskViewModel CreateAddTask()
    {
        return new AddTaskViewModel(_repository, _categoryRepository);
    }

    public AddTaskViewModel? CreateEditTask(Guid taskId)
    {
        try
        {
            var task = _repository.GetById(taskId);

            if (task is null)
                return null;

            return new AddTaskViewModel(_repository, _categoryRepository, task);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
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
        var newCards = tasks.Select(task => new TaskCard(task)).ToList();
        if (newCards.SequenceEqual(cards))
            return;

        cards.Clear();
        foreach (var card in newCards)
            cards.Add(card);
    }
}
