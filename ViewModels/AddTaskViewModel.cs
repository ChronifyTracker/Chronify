using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Chronify.ViewModels;

public partial class AddTaskViewModel : ObservableObject
{
    private static readonly string[] CategoryColors =
    [
        "#F34F78", "#5B9BF3", "#4CC38A", "#F2B45A",
        "#A78BFA", "#3CC8C8", "#F2854A", "#E06BD6"
    ];

    private static readonly CategoryOption NoCategory = new("Без категории");
    private static readonly CategoryOption NewCategory = new("+ Добавить категорию", isAddNew: true);

    private readonly ITaskRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly TaskItem? _task;

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
    public partial TimeSpan? DueTime { get; set; } = TimeSpan.FromHours(18);

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public ObservableCollection<CategoryOption> Categories { get; } = [NoCategory];
    public bool CategoriesLoaded { get; }
    public bool IsAddingCategory => SelectedCategory == NewCategory;
    public string Heading => _task is null ? "Новая задача" : "Изменить задачу";
    public string SaveText => _task is null ? "Добавить" : "Сохранить";

    public AddTaskViewModel(ITaskRepository repository, ICategoryRepository categoryRepository, TaskItem? task = null)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _task = task;

        try
        {
            foreach (var category in _categoryRepository.GetAll())
                Categories.Add(new CategoryOption(category.Name, category));

            Categories.Add(NewCategory);
            CategoriesLoaded = true;
        }
        catch (Exception)
        {
            CategoriesLoaded = false;
        }

        if (task is null)
            return;

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
            ErrorMessage = "Введите название.";
            return false;
        }

        if (Minutes is null || Minutes != decimal.Truncate(Minutes.Value))
        {
            ErrorMessage = "Укажите целое число минут.";
            return false;
        }

        if (IsAddingCategory && !AddCategory())
            return false;

        DateTime? dueDate = null;
        if (DueDate is DateTime date)
            dueDate = date.Date + (DueTime ?? TimeSpan.FromHours(18));

        try
        {
            var task = _task ?? new TaskItem(Title);

            task.SetTitle(Title);
            task.SetDescription(Description ?? "");
            task.SetDueDate(dueDate);
            task.SetEstimatedMinutes((int)Minutes.Value);
            task.SetPriority((TaskPriority)PriorityIndex);
            if (CategoriesLoaded)
                task.SetCategory(SelectedCategory?.Category);

            if (_task is null)
                _repository.Add(task);
            else if (!_repository.Update(task))
            {
                ErrorMessage = "Задача уже удалена.";
                return false;
            }

            return true;
        }
        catch (ModelValidationException ex)
        {
            ErrorMessage = ex.Error switch
            {
                ModelError.TaskTitleRequired => "Введите название.",
                ModelError.TaskCompleted => "Завершённую задачу нельзя изменить.",
                ModelError.EstimatedMinutesOutOfRange =>
                    $"Время на задачу — от {TaskItem.MinEstimatedMinutes} до {TaskItem.MaxEstimatedMinutes} мин.",
                _ => "Проверьте данные задачи."
            };
            return false;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось сохранить задачу.";
            return false;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName != nameof(ErrorMessage))
            ErrorMessage = null;
    }

    private bool AddCategory()
    {
        try
        {
            var savedCategories = Categories.Count(option => option.Category is not null);
            var color = CategoryColors[savedCategories % CategoryColors.Length];
            var category = new Category(NewCategoryName ?? "", color);
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
                ? "Такая категория уже есть."
                : "Введите название категории.";
            return false;
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось сохранить категорию.";
            return false;
        }
    }
}
