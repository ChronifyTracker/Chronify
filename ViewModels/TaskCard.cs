namespace Chronify.ViewModels;

public record TaskCard
{
    public Guid Id { get; }
    public string Title { get; }
    public string Description { get; }
    public bool HasDescription => Description.Length > 0;
    public string? Tooltip => HasDescription ? Description : null;
    public string Details { get; }
    public string Priority { get; }
    public string Status { get; }
    public string DueDate { get; }
    public string Category { get; }
    public string EstimatedMinutes { get; }
    public bool IsOverdue { get; }
    public bool IsDueSoon { get; }

    public TaskCard(TaskItem task)
    {
        Id = task.Id;
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
        DueDate = task.DueDate?.ToString("dd.MM.yyyy · HH:mm") ?? "Без срока";
        Category = task.Category?.Name ?? "";
        EstimatedMinutes = task.EstimatedMinutes > 0 ? task.EstimatedMinutes.ToString() : "";
        IsOverdue = task.IsOverdue;
        IsDueSoon = task.IsDueSoon;

        Details = DueDate;
        if (IsOverdue)
            Details = "Просрочена · " + Details;
        if (EstimatedMinutes.Length > 0)
            Details += $" · {EstimatedMinutes} мин";
        if (Category.Length > 0)
            Details += $" · {Category}";
    }
}
