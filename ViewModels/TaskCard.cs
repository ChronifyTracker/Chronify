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
    public string? CategoryColor { get; }
    public bool HasCategory => Category.Length > 0;
    public string Estimate { get; }
    public bool IsOverdue { get; }
    public bool IsDueSoon { get; }
    public bool CanStart { get; }
    public bool CanChange { get; }

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
        CategoryColor = task.Category?.Color;
        Estimate = DurationText.From(task.EstimatedMinutes);
        IsOverdue = task.IsOverdue;
        IsDueSoon = task.IsDueSoon;
        CanStart = task.Status == TaskStatus.New;
        CanChange = task.Status != TaskStatus.Completed;

        if (IsOverdue)
            Status = "Просрочена";

        Details = DueDate;
        if (Estimate.Length > 0)
            Details += $" · {Estimate}";
    }
}
