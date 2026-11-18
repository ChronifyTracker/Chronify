namespace Chronify.Services;

public class TaskFilter
{
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public TaskStatusFilter Status { get; init; }
    public TaskDueFilter Due { get; init; }

    public List<TaskItem> Apply(IEnumerable<TaskItem> tasks)
    {
        return tasks.Where(Matches).ToList();
    }

    private bool Matches(TaskItem task)
    {
        return MatchesSearch(task)
            && MatchesCategory(task)
            && MatchesStatus(task)
            && MatchesDue(task);
    }

    private bool MatchesSearch(TaskItem task)
    {
        if (string.IsNullOrWhiteSpace(Search))
            return true;

        return task.Title.Contains(Search.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesCategory(TaskItem task)
    {
        if (CategoryId is null)
            return true;

        return task.CategoryId == CategoryId;
    }

    private bool MatchesStatus(TaskItem task)
    {
        return Status switch
        {
            TaskStatusFilter.New => task.Status == TaskStatus.New && !task.IsOverdue,
            TaskStatusFilter.InProgress => task.Status == TaskStatus.InProgress && !task.IsOverdue,
            TaskStatusFilter.Completed => task.Status == TaskStatus.Completed,
            TaskStatusFilter.Overdue => task.IsOverdue,
            _ => true
        };
    }

    private bool MatchesDue(TaskItem task)
    {
        if (Due == TaskDueFilter.All)
            return true;

        if (task.DueDate is not DateTime dueDate)
            return false;

        var days = Due == TaskDueFilter.Today ? 1 : 3;

        return dueDate >= DateTime.Today && dueDate < DateTime.Today.AddDays(days);
    }
}
