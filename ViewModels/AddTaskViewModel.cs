using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Chronify.ViewModels;

public partial class AddTaskViewModel : LocalizedObservableObject
{
    private const string DefaultCategoryColor = "#F34F78";

    private static readonly CategoryOption NoCategory = new("", translationKey: "Categories.None");
    private static readonly CategoryOption NewCategory = new("", isAddNew: true, translationKey: "Categories.AddOption");

    private readonly ITaskRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly TaskItem? _task;

    public IReadOnlyList<LocalizedOption> Priorities { get; } =
    [
        new("Priority.Deferred"),
        new("Priority.Low"),
        new("Priority.Medium"),
        new("Priority.High"),
        new("Priority.Critical")
    ];

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial string? Description { get; set; }

    [ObservableProperty]
    public partial int PriorityIndex { get; set; } = (int)TaskPriority.Medium;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAddingCategory))]
    public partial CategoryOption? SelectedCategory { get; set; } = NoCategory;

    [ObservableProperty]
    public partial string? NewCategoryName { get; set; }

    [ObservableProperty]
    public partial decimal? Minutes { get; set; } = 0;

    [ObservableProperty]
    public partial DateTime? DueDate { get; set; }

    [ObservableProperty]
    public partial string? DateText { get; set; }

    [ObservableProperty]
    public partial TimeSpan? DueTime { get; set; } = TimeSpan.FromHours(18);

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public ObservableCollection<CategoryOption> Categories { get; } = [NoCategory];
    public bool CategoriesLoaded { get; }
    public bool IsAddingCategory => SelectedCategory == NewCategory;

    public string Heading => _task is null ? Texts.Get("Tasks.NewHeading") : Texts.Get("Tasks.EditHeading");

    public string SaveText => _task is null ? Texts.Get("Common.Add") : Texts.Get("Common.Save");

    public override void RefreshLanguage()
    {
        ErrorMessage = null;
        base.RefreshLanguage();
    }

    public AddTaskViewModel(
        ITaskRepository repository,
        ICategoryRepository categoryRepository,
        TaskItem? task = null
    )
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _task = task;

        try
        {
            foreach (var category in _categoryRepository.GetAll())
            {
                Categories.Add(new CategoryOption(category.Name, category));
            }

            Categories.Add(NewCategory);
            CategoriesLoaded = true;
        }
        catch (Exception)
        {
            CategoriesLoaded = false;
        }

        if (task is null)
        {
            return;
        }

        Title = task.Title;
        Description = task.Description;
        PriorityIndex = (int)task.Priority;
        Minutes = task.EstimatedMinutes;
        DueDate = task.DueDate?.Date;
        DueTime = task.DueDate?.TimeOfDay ?? DueTime;
        SelectedCategory = Categories.FirstOrDefault(c => c.Category?.Id == task.CategoryId) ?? NoCategory;
    }

    public bool Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = Texts.Get("Errors.TitleRequired");
            return false;
        }

        if (Minutes is null || Minutes != decimal.Truncate(Minutes.Value))
        {
            ErrorMessage = Texts.Get("Errors.IntegerMinutes");
            return false;
        }

        if (Minutes < TaskItem.MinEstimatedMinutes || Minutes > TaskItem.MaxEstimatedMinutes)
        {
            ErrorMessage = Texts.Format("Errors.TaskMinutesRange", TaskItem.MinEstimatedMinutes, TaskItem.MaxEstimatedMinutes);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(DateText) &&
            (!DateTime.TryParse(
            DateText,
            LocalizationService.Current.Culture,
            DateTimeStyles.None,
            out var parsed
        )
                || DueDate is null || parsed.Date != DueDate.Value.Date))
        {
            ErrorMessage = Texts.Get("Errors.Date");
            return false;
        }

        DateTime? dueDate = null;
        if (DueDate is DateTime date)
        {
            dueDate = date.Date + (DueTime ?? TimeSpan.FromHours(18));
        }

        try
        {
            var task = _task?.Copy() ?? new TaskItem(Title);

            task.SetTitle(Title);
            task.SetDescription(Description ?? "");
            task.SetDueDate(dueDate);
            task.SetEstimatedMinutes((int)Minutes.Value);
            task.SetPriority((TaskPriority)PriorityIndex);
            if (IsAddingCategory && !AddCategory())
            {
                return false;
            }

            if (CategoriesLoaded)
            {
                task.SetCategory(SelectedCategory?.Category);
            }

            if (_task is null)
            {
                _repository.Add(task);
            }
            else if (!_repository.Update(task))
            {
                ErrorMessage = Texts.Get("Errors.TaskDeleted");
                return false;
            }

            return true;
        }
        catch (ModelValidationException ex)
        {
            ErrorMessage = ex.Error switch
            {
                ModelError.TaskTitleRequired => Texts.Get("Errors.TitleRequired"),
                ModelError.TaskCompleted => Texts.Get("Errors.TaskCompleted"),
                ModelError.DueDateInPast => Texts.Get("Errors.PastDueDate"),
                ModelError.TaskPriorityInvalid => Texts.Get("Errors.PriorityRequired"),
                ModelError.EstimatedMinutesOutOfRange =>
                    Texts.Format("Errors.TaskMinutesRange", TaskItem.MinEstimatedMinutes, TaskItem.MaxEstimatedMinutes),
                _ => Texts.Get("Errors.TaskInput")
            };
            return false;
        }
        catch (Exception)
        {
            ErrorMessage = Texts.Get("Errors.TaskSave");
            return false;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName != nameof(ErrorMessage))
        {
            ErrorMessage = null;
        }
    }

    private bool AddCategory()
    {
        try
        {
            var category = new Category(NewCategoryName ?? "", DefaultCategoryColor);
            var option = new CategoryOption(category.Name, category);

            _categoryRepository.Add(category);
            Categories.Insert(Categories.Count - 1, option);
            SelectedCategory = option;
            NewCategoryName = null;
            return true;
        }
        catch (ModelValidationException ex)
        {
            ErrorMessage = ex.Error == ModelError.CategoryNameTaken
                ? Texts.Get("Errors.CategoryExistsShort")
                : Texts.Get("Errors.CategoryName");
            return false;
        }
        catch (Exception)
        {
            ErrorMessage = Texts.Get("Errors.CategorySave");
            return false;
        }
    }
}
