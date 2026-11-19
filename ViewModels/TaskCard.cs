namespace Chronify.ViewModels;

public record TaskCard
{
    public Guid Id { get; }
    public string Title { get; }
    public string Description { get; }
    public bool HasDescription => Description.Length > 0;

    public bool HasCategory => Category.Length > 0;

    public string Estimate { get; }
    public string AssignedByName { get; }
    public bool CanEdit { get; }
    public bool CanReopen { get; }
    public bool CanDelete { get; }
    public bool CanComplete { get; }
    public string? Tooltip => HasDescription ? Description : null;

    public string Details { get; }
    public string Priority { get; }
    public string Status { get; }
    public string DueDate { get; }
    public string Category { get; }
    public string CategoryColor { get; }
    public int ActualMinutes { get; }
    public int DynamicPriority { get; }
    public string EstimatedMinutes { get; }
    public bool IsOverdue { get; }
    public bool IsDueSoon { get; }
    public bool CanStart { get; }
    public bool CanChange { get; }

    public TaskCard(TaskItem task, int actualMinutes = 0)
    {
        Id = task.Id;
        Title = task.Title;
        Description = task.Description;
        Priority = task.Priority switch
        {
            TaskPriority.Critical => Texts.Get("Priority.Critical"),
            TaskPriority.High => Texts.Get("Priority.High"),
            TaskPriority.Medium => Texts.Get("Priority.Medium"),
            TaskPriority.Low => Texts.Get("Priority.Low"),
            _ => Texts.Get("Priority.Deferred")
        };
        Status = task.Status switch
        {
            TaskStatus.InProgress => Texts.Get("Status.InProgress"),
            TaskStatus.Completed => Texts.Get("Status.Completed"),
            _ => Texts.Get("Status.New")
        };
        DueDate = task.DueDate is DateTime dueDate ? Texts.DateTime(dueDate) : Texts.Get("Tasks.NoDueDate");
        Category = task.Category?.Name ?? "";
        CategoryColor = task.Category?.Color ?? "#A29AA4";
        ActualMinutes = actualMinutes;
        DynamicPriority = Services.DynamicPriorityCalculator.Calculate(task, DateTime.Now);
        EstimatedMinutes = task.EstimatedMinutes > 0 ? task.EstimatedMinutes.ToString() : "";
        Estimate = DurationText.From(task.EstimatedMinutes);
        IsOverdue = task.IsOverdue;
        IsDueSoon = task.IsDueSoon;
        CanStart = task.Status == TaskStatus.New && task.GroupAssignmentId is null;
        CanChange = task.Status != TaskStatus.Completed;
        CanEdit = CanChange && task.GroupAssignmentId is null;
        CanReopen = task.GroupAssignmentId is null;
        CanDelete = task.GroupAssignmentId is null;
        CanComplete = CanChange && (task.GroupAssignmentId is null || task.Status == TaskStatus.InProgress);
        AssignedByName = task.AssignedByName;

        if (IsOverdue)
        {
            Status = Texts.Get("Status.Overdue");
        }

        Details = DueDate;
        if (Estimate.Length > 0)
        {
            Details += $" · {Estimate}";
        }

        if (Category.Length > 0)
        {
            Details += $" · {Category}";
        }

        if (AssignedByName.Length > 0)
        {
            Details += Texts.Format("Tasks.AssignedBySuffix", AssignedByName);
        }
    }
}
