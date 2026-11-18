using CommunityToolkit.Mvvm.ComponentModel;

namespace Chronify.ViewModels;

public partial class AddTaskViewModel : ObservableObject
{
    private readonly ITaskRepository _repository;

    [ObservableProperty]
    public partial string? Title { get; set; }

    [ObservableProperty]
    public partial string? Description { get; set; }

    [ObservableProperty]
    public partial int PriorityIndex { get; set; } = 2;

    [ObservableProperty]
    public partial decimal? Minutes { get; set; } = 0;

    [ObservableProperty]
    public partial DateTime? DueDate { get; set; }

    [ObservableProperty]
    public partial string? DueDateText { get; set; }

    [ObservableProperty]
    public partial TimeSpan? DueTime { get; set; } = TimeSpan.FromHours(18);

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public AddTaskViewModel(ITaskRepository repository)
    {
        _repository = repository;
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

        if (!string.IsNullOrWhiteSpace(DueDateText) && DueDate is null)
        {
            ErrorMessage = "Проверьте дату.";
            return false;
        }

        DateTime? dueDate = null;
        if (DueDate is DateTime date)
            dueDate = date.Date + (DueTime ?? TimeSpan.FromHours(18));

        try
        {
            var task = new TaskItem(
                Title,
                Description ?? "",
                dueDate: dueDate,
                estimatedMinutes: (int)Minutes.Value,
                priority: SelectedPriority());

            _repository.Add(task);
            return true;
        }
        catch (ModelValidationException ex)
        {
            ErrorMessage = ex.Error switch
            {
                ModelError.TaskTitleRequired => "Введите название.",
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

    private TaskPriority SelectedPriority() => PriorityIndex switch
    {
        0 => TaskPriority.Postponed,
        1 => TaskPriority.Low,
        3 => TaskPriority.High,
        4 => TaskPriority.Critical,
        _ => TaskPriority.Medium
    };
}
