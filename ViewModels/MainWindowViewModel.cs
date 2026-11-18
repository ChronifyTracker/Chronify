using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Threading;
using Chronify.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Chronify.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private static readonly CategoryOption AllCategories = new("Все категории");

    private readonly ITaskRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMinutes(1) };
    private List<TaskItem> _tasks = [];

    [ObservableProperty]
    public partial string DateText { get; set; } = "";

    [ObservableProperty]
    public partial bool LoadFailed { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsHomePage { get; set; } = true;

    [ObservableProperty]
    public partial string? SearchText { get; set; }

    [ObservableProperty]
    public partial CategoryOption? SelectedCategory { get; set; } = AllCategories;

    [ObservableProperty]
    public partial int StatusIndex { get; set; } = (int)TaskStatusFilter.All;

    [ObservableProperty]
    public partial int DueIndex { get; set; } = (int)TaskDueFilter.All;

    [ObservableProperty]
    public partial int SortIndex { get; set; } = (int)TaskSortOrder.Urgency;

    public ObservableCollection<TaskCard> UrgentTasks { get; } = [];
    public ObservableCollection<TaskCard> NewTasks { get; } = [];
    public ObservableCollection<TaskCard> AllTasks { get; } = [];
    public ObservableCollection<TaskCard> FilteredTasks { get; } = [];
    public ObservableCollection<CategoryOption> Categories { get; } = [AllCategories];

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
            _tasks = _repository.GetAll();

            var urgent = _tasks.Where(task => task.IsUrgent);
            var newTasks = new TaskFilter { Status = TaskStatusFilter.New }.Apply(_tasks);

            Fill(UrgentTasks, TaskSorter.SortByUrgency(urgent));
            Fill(NewTasks, newTasks);
            Fill(AllTasks, _tasks);
            ApplyFilters();
            LoadFailed = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            LoadFailed = true;
            return;
        }

        LoadCategories();
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

            if (task is null || task.Status == TaskStatus.Completed)
            {
                return null;
            }

            return new AddTaskViewModel(_repository, _categoryRepository, task);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    public void StartTask(Guid taskId)
    {
        ChangeTask(taskId, task => task.StartTask());
    }

    public void CompleteTask(Guid taskId)
    {
        ChangeTask(taskId, task => task.CompleteTask());
    }

    public void ReopenTask(Guid taskId)
    {
        ChangeTask(taskId, task => task.ReopenTask());
    }

    public void DeleteTask(Guid taskId)
    {
        ErrorMessage = null;

        try
        {
            _repository.Delete(taskId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            ErrorMessage = "Не удалось удалить задачу.";
        }

        Refresh();
    }

    [RelayCommand]
    private void ShowHome()
    {
        IsHomePage = true;
        ErrorMessage = null;
        Refresh();
    }

    [RelayCommand]
    private void ShowAll()
    {
        IsHomePage = false;
        ErrorMessage = null;
        Refresh();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = null;
        SelectedCategory = AllCategories;
        StatusIndex = (int)TaskStatusFilter.All;
        DueIndex = (int)TaskDueFilter.All;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is nameof(SearchText) or nameof(SelectedCategory)
            or nameof(StatusIndex) or nameof(DueIndex) or nameof(SortIndex))
        {
            ApplyFilters();
        }
    }

    private void ApplyFilters()
    {
        var filter = new TaskFilter
        {
            Search = SearchText,
            CategoryId = SelectedCategory?.Category?.Id,
            Status = (TaskStatusFilter)StatusIndex,
            Due = (TaskDueFilter)DueIndex
        };

        var filteredTasks = filter.Apply(_tasks);

        Fill(FilteredTasks, TaskSorter.Sort(filteredTasks, (TaskSortOrder)SortIndex));
    }

    private void LoadCategories()
    {
        try
        {
            var categories = _categoryRepository.GetAll();

            for (var i = 0; i < categories.Count; i++)
            {
                var category = categories[i];

                if (!Categories.Any(option => option.Category?.Id == category.Id))
                {
                    Categories.Insert(i + 1, new CategoryOption(category.Name, category));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            ErrorMessage = "Не удалось загрузить категории.";
        }
    }

    private void ChangeTask(Guid taskId, Action<TaskItem> change)
    {
        ErrorMessage = null;

        try
        {
            var task = _repository.GetById(taskId);

            if (task is not null)
            {
                change(task);
                _repository.Update(task);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            ErrorMessage = "Не удалось изменить задачу.";
        }

        Refresh();
    }

    private static void Fill(ObservableCollection<TaskCard> cards, IEnumerable<TaskItem> tasks)
    {
        var newCards = tasks.Select(task => new TaskCard(task)).ToList();
        if (newCards.SequenceEqual(cards))
        {
            return;
        }

        cards.Clear();
        foreach (var card in newCards)
        {
            cards.Add(card);
        }
    }
}
