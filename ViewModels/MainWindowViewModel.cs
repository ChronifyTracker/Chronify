using System.Collections.ObjectModel;
using Chronify.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Chronify.ViewModels;

public partial class MainWindowViewModel : PageViewModel
{
    private readonly ITaskRepository _tasks;
    private readonly ICategoryRepository _categories;
    private readonly WorkSessionRepository _sessions;
    private readonly FilterPresetRepository _presets;
    private bool _changingFilters;
    private readonly Guid _userId;
    private readonly Func<AppDbContext>? _createContext;
    private readonly AuthenticationService _authentication;
    public GroupsViewModel GroupsPage { get; }
    public ImportViewModel ImportPage { get; }
    public bool HasClassroom => AccountPage.Integrations.HasClassroom;

    public bool HasNotion => AccountPage.Integrations.HasNotion;

    public CategoriesViewModel CategoriesPage { get; }
    public PlanningViewModel PlanningPage { get; }
    public StatisticsViewModel StatisticsPage { get; }
    public WorkTimeViewModel WorkTimePage { get; }
    public AccountViewModel AccountPage { get; }
    public string? SessionNotice => _authentication.SessionNotice;
    public event Action? LogoutRequested;

    [ObservableProperty]
    public partial int SelectedPage { get; set; }
    [ObservableProperty]
    public partial string DateText { get; set; } = "";
    [ObservableProperty]
    public partial bool LoadFailed { get; set; }
    [ObservableProperty]
    public partial string SearchText { get; set; } = "";
    [ObservableProperty]
    public partial CategoryOption? FilterCategory { get; set; }
    [ObservableProperty]
    public partial int FilterStatusIndex { get; set; }
    [ObservableProperty]
    public partial int FilterPriorityIndex { get; set; }
    [ObservableProperty]
    public partial int FilterDateIndex { get; set; }
    [ObservableProperty]
    public partial int SortIndex { get; set; }
    [ObservableProperty]
    public partial string PresetName { get; set; } = "";
    [ObservableProperty]
    public partial FilterPreset? SelectedPreset { get; set; }

    public bool IsHomePage => SelectedPage == 0;

    public bool IsTasksPage => SelectedPage == 1;

    public bool IsCategoriesPage => SelectedPage == 2;

    public bool IsPlanningPage => SelectedPage == 3;

    public bool IsStatisticsPage => SelectedPage == 4;

    public bool IsWorkTimePage => SelectedPage == 5;

    public bool IsAccountPage => SelectedPage == 6;

    public bool IsGroupsPage => SelectedPage == 7;

    public bool IsImportPage => SelectedPage == 8;

    public string PageTitle
    {
        get
        {
            string[] keys =
            [
                "Navigation.Home", "Navigation.Tasks", "Navigation.Categories",
                "Navigation.Planning", "Navigation.Statistics", "Navigation.WorkTime",
                "Navigation.Account", "Navigation.Groups", "Navigation.Import"
            ];
            return Texts.Get(keys[SelectedPage]);
        }
    }

    public IReadOnlyList<LocalizedOption> StatusFilters { get; } =
    [
        new("Filters.AllStatuses"),
        new("Filters.New"),
        new("Status.InProgress"),
        new("Filters.Completed")
    ];
    public IReadOnlyList<LocalizedOption> PriorityFilters { get; } =
    [
        new("Filters.AllPriorities"),
        new("Priority.Deferred"),
        new("Priority.Low"),
        new("Priority.Medium"),
        new("Priority.High"),
        new("Priority.Critical")
    ];
    public IReadOnlyList<LocalizedOption> DateFilters { get; } =
    [
        new("Filters.AllDates"),
        new("Filters.Today"),
        new("Filters.NextThreeDays"),
        new("Filters.Overdue")
    ];
    public IReadOnlyList<LocalizedOption> SortOptions { get; } =
    [
        new("Sort.Edf"),
        new("Sort.DynamicPriority"),
        new("Tasks.Importance"),
        new("Sort.Newest"),
        new("Common.Title"),
        new("Tasks.Category"),
        new("Sort.WeightedUrgency")
    ];
    public ObservableCollection<CategoryOption> FilterCategories { get; } = [];
    public ObservableCollection<FilterPreset> Presets { get; } = [];
    public ObservableCollection<TaskCard> UrgentTasks { get; } = [];
    public ObservableCollection<TaskCard> NewTasks { get; } = [];
    public ObservableCollection<TaskCard> AllTasks { get; } = [];

    public MainWindowViewModel(
        UserAccount user,
        ITaskRepository tasks,
        ICategoryRepository categories,
        WorkSessionRepository sessions,
        FilterPresetRepository presets,
        UserPreferencesRepository preferences,
        AuthenticationService authentication,
        Func<AppDbContext>? createContext = null
    )
    {
        _tasks = tasks;
        _userId = user.Id;
        _createContext = createContext;
        _categories = categories;
        _sessions = sessions;
        _presets = presets;
        _authentication = authentication;
        CategoriesPage = new CategoriesViewModel(categories);
        PlanningPage = new PlanningViewModel(tasks, preferences);
        StatisticsPage = new StatisticsViewModel(tasks, sessions);
        WorkTimePage = new WorkTimeViewModel(user.Id, tasks, sessions);
        AccountPage = new AccountViewModel(user, authentication, createContext);
        GroupsPage = new GroupsViewModel(user, createContext);
        ImportPage = new ImportViewModel(user.Id, createContext);
        GroupsPage.DataChanged += Refresh;
        ImportPage.DataChanged += Refresh;
        AccountPage.Integrations.ConnectionsChanged += () =>
        {
            OnPropertyChanged(nameof(HasClassroom));
            OnPropertyChanged(nameof(HasNotion));
        };
        CategoriesPage.DataChanged += Refresh;
        WorkTimePage.DataChanged += Refresh;
        AccountPage.LogoutRequested += () => LogoutRequested?.Invoke();
        string code = preferences.Get().LanguageCode;
        LocalizationService.Current.ChangeLanguage(LocalizationService.IsSupported(code) ? code : "ru");
        Refresh();
    }

    public override void RefreshLanguage()
    {
        base.RefreshLanguage();
        DateText = DateTime.Now.ToString("dddd, d MMMM", LocalizationService.Current.Culture);
        Run(() =>
        {
            var overview = new TaskOverviewService(_tasks).GetOverview(DateTime.Now);
            Fill(UrgentTasks, overview.Urgent);
            Fill(NewTasks, overview.New);
            Fill(AllTasks, _tasks.GetAll(BuildQuery()));
        });
    }

    partial void OnSelectedPageChanged(int value)
    {
        foreach (string property in new[] { nameof(IsHomePage), nameof(IsTasksPage), nameof(IsCategoriesPage),
            nameof(IsPlanningPage), nameof(IsStatisticsPage), nameof(IsWorkTimePage), nameof(IsAccountPage),
            nameof(IsGroupsPage), nameof(IsImportPage), nameof(PageTitle) })
        {
            OnPropertyChanged(property);
        }
    }
    partial void OnSearchTextChanged(string value)
    {
        RefreshFilteredTasks();
    }

    partial void OnFilterCategoryChanged(CategoryOption? value)
    {
        RefreshFilteredTasks();
    }

    partial void OnFilterStatusIndexChanged(int value)
    {
        RefreshFilteredTasks();
    }

    partial void OnFilterPriorityIndexChanged(int value)
    {
        RefreshFilteredTasks();
    }

    partial void OnFilterDateIndexChanged(int value)
    {
        RefreshFilteredTasks();
    }

    partial void OnSortIndexChanged(int value)
    {
        RefreshFilteredTasks();
    }

    public void Refresh()
    {
        DateText = DateTime.Now.ToString("dddd, d MMMM", LocalizationService.Current.Culture);
        Run(() =>
        {
            _changingFilters = true;
            try
            {
                Guid? categoryId = FilterCategory?.Category?.Id;
                FilterCategories.Clear();
                FilterCategories.Add(new CategoryOption("", translationKey: "Filters.AllCategories"));
                foreach (var category in _categories.GetAll())
                {
                    FilterCategories.Add(new CategoryOption(category.Name, category));
                }
                FilterCategory = FilterCategories.FirstOrDefault(c => c.Category?.Id == categoryId) ?? FilterCategories[0];
                Presets.Clear();
                foreach (var preset in _presets.GetAll())
                {
                    Presets.Add(preset);
                }
            }
            finally
            {
                _changingFilters = false;
            }
            var overview = new TaskOverviewService(_tasks).GetOverview(DateTime.Now);
            Fill(UrgentTasks, overview.Urgent);
            Fill(NewTasks, overview.New);
            Fill(AllTasks, _tasks.GetAll(BuildQuery()));
            LoadFailed = false;
        });
        if (ErrorMessage is not null)
        {
            LoadFailed = true;
        }
    }

    public TaskQuery BuildQuery()
    {
        return new()
        {
            Search = SearchText,
            CategoryId = FilterCategory?.Category?.Id,
            Status = FilterStatusIndex == 0 ? null : (TaskStatus)(FilterStatusIndex - 1),
            Priority = FilterPriorityIndex == 0 ? null : (TaskPriority)(FilterPriorityIndex - 1),
            DateFilter = (TaskDateFilter)FilterDateIndex,
            SortBy = SortIndex switch
            {
                1 => TaskSortBy.DynamicPriority,
                2 => TaskSortBy.Priority,
                3 => TaskSortBy.CreatedAt,
                4 => TaskSortBy.Title,
                5 => TaskSortBy.Category,
                6 => TaskSortBy.WeightedUrgency,
                _ => TaskSortBy.Edf
            },
            SortDescending = SortIndex is 2 or 3
        };
    }

    private void RefreshFilteredTasks()
    {
        if (!_changingFilters)
        {
            Run(() => Fill(AllTasks, _tasks.GetAll(BuildQuery())));
        }
    }

    [RelayCommand]
    private void ResetFilters()
    {
        _changingFilters = true;
        SearchText = "";
        FilterCategory = FilterCategories.FirstOrDefault();
        FilterStatusIndex = 0;
        FilterPriorityIndex = 0;
        FilterDateIndex = 0;
        SortIndex = 0;
        _changingFilters = false;
        RefreshFilteredTasks();
    }

    [RelayCommand]
    private void SavePreset()
    {
        if (Run(() => _presets.Save(PresetName, BuildQuery())))
        {
            PresetName = "";
            Refresh();
        }
    }
    [RelayCommand]
    private void DeletePreset()
    {
        if (SelectedPreset is null)
        {
            ErrorMessage = Texts.Get("Errors.SelectPreset");
            return;
        }
        if (Run(() => _presets.Delete(SelectedPreset.Id)))
        {
            Refresh();
        }
    }
    [RelayCommand]
    private void ApplyPreset()
    {
        if (SelectedPreset is null)
        {
            return;
        }

        var query = SelectedPreset.ToQuery();
        _changingFilters = true;
        SearchText = query.Search ?? "";
        FilterCategory = FilterCategories.FirstOrDefault(c => c.Category?.Id == query.CategoryId) ?? FilterCategories.First();
        FilterStatusIndex = query.Status is TaskStatus status ? (int)status + 1 : 0;
        FilterPriorityIndex = query.Priority is TaskPriority priority ? (int)priority + 1 : 0;
        FilterDateIndex = (int)query.DateFilter;
        SortIndex = query.SortBy switch
        {
            TaskSortBy.DynamicPriority => 1,
            TaskSortBy.Priority => 2,
            TaskSortBy.CreatedAt => 3,
            TaskSortBy.Title => 4,
            TaskSortBy.Category => 5,
            TaskSortBy.WeightedUrgency => 6,
            _ => 0
        };
        _changingFilters = false;
        RefreshFilteredTasks();
    }

    public AddTaskViewModel CreateAddTask()
    {
        return new(_tasks, _categories);
    }

    public TaskDetailsViewModel? TaskDetails(Guid id)
    {
        var task = _tasks.GetById(id);
        return task is null ? null : new TaskDetailsViewModel(task, _userId, _createContext);
    }
    public AddTaskViewModel? CreateEditTask(Guid id)
    {
        var task = _tasks.GetById(id);
        return task is null || task.Status == TaskStatus.Completed || task.GroupAssignmentId is not null ? null : new AddTaskViewModel(
            _tasks,
            _categories,
            task
        );
    }
    public void StartTask(Guid id)
    {
        ChangeTask(id, t => t.StartTask());
    }

    public void CompleteTask(Guid id)
    {
        ChangeTask(id, t => t.CompleteTask());
    }

    public void ReopenTask(Guid id)
    {
        ChangeTask(id, t => t.ReopenTask());
    }

    public void DeleteTask(Guid id)
    {
        if (Run(() => _tasks.Delete(id)))
        {
            Refresh();
        }
    }
    private void ChangeTask(Guid id, Action<TaskItem> change)
    {
        if (Run(() =>
        {
            var task = _tasks.GetById(id) ?? throw new InvalidOperationException(Texts.Get("Errors.TaskDeleted"));
            change(task);
            if (!_tasks.Update(task))
            {
                throw new InvalidOperationException(Texts.Get("Errors.TaskDeleted"));
            }
        }))
        {
            Refresh();
        }
    }
    [RelayCommand]
    private void ShowHome()
    {
        SelectedPage = 0;
        Refresh();
    }
    [RelayCommand]
    private void ShowAll()
    {
        SelectedPage = 1;
        Refresh();
    }
    [RelayCommand]
    private void ShowCategories()
    {
        SelectedPage = 2;
        CategoriesPage.Refresh();
    }
    [RelayCommand]
    private void ShowPlanning()
    {
        SelectedPage = 3;
        PlanningPage.Refresh();
    }
    [RelayCommand]
    private void ShowStatistics()
    {
        SelectedPage = 4;
        StatisticsPage.Refresh();
    }
    [RelayCommand]
    private void ShowWorkTime()
    {
        SelectedPage = 5;
        WorkTimePage.Refresh();
    }
    [RelayCommand]
    private void ShowAccount()
    {
        SelectedPage = 6;
    }

    [RelayCommand]
    private async Task ShowGroups()
    {
        SelectedPage = 7;
        GroupsPage.RefreshCache();
        await GroupsPage.RefreshAutomatically();
    }
    [RelayCommand]
    private Task ImportClassroom()
    {
        return OpenImport("Classroom");
    }

    [RelayCommand]
    private Task ImportNotion()
    {
        return OpenImport("Notion");
    }

    private async Task OpenImport(string provider)
    {
        SelectedPage = 8;
        await ImportPage.Open(provider);
    }

    private void Fill(ObservableCollection<TaskCard> cards, IEnumerable<TaskItem> tasks)
    {
        var totals = _sessions.GetAll().GroupBy(s => s.TaskId).ToDictionary(g => g.Key, g => g.Sum(s => s.Minutes));
        var values = tasks.Select(t => new TaskCard(t, totals.GetValueOrDefault(t.Id))).ToList();
        if (values.SequenceEqual(cards))
        {
            return;
        }

        cards.Clear();
        foreach (var value in values)
        {
            cards.Add(value);
        }
    }
}
