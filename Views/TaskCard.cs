namespace Chronify.Views;

// Только текст для карточки. Сама задача и её правила остаются в TaskItem.
public class TaskCard
{
    public string Title { get; }
    public string Description { get; }
    public bool HasDescription => Description.Length > 0;
    public string Priority { get; }
    public string Status { get; }
    public string Details { get; }

    public TaskCard(TaskItem task)
    {
        Title = task.Title;
        Description = task.Description;
        Priority = task.Priority switch
        {
            TaskPriority.Critical => "Критический",
            TaskPriority.High => "Высокий",
            TaskPriority.Medium => "Средний",
            TaskPriority.Low => "Низкий",
            _ => "Отложено"
        };
        Status = task.Status switch
        {
            TaskStatus.InProgress => "В работе",
            TaskStatus.Completed => "Завершена",
            _ => "Новая"
        };
        Details = task.DueDate?.ToString("dd.MM.yyyy · HH:mm") ?? "Без срока";
        if (task.IsOverdue)
            Details = "Просрочена · " + Details;
        if (task.EstimatedMinutes > 0)
            Details += $" · {task.EstimatedMinutes} мин";
        if (task.Category is not null)
            Details += $" · {task.Category.Name}";
    }
}
