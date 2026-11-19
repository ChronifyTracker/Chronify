namespace Chronify.Services;

public sealed record TaskOverview(
    IReadOnlyList<TaskItem> All,
    IReadOnlyList<TaskItem> Urgent,
    IReadOnlyList<TaskItem> New
);

public sealed class TaskOverviewService(ITaskRepository repository)
{
    public TaskOverview GetOverview(DateTime now)
    {
        var tasks = repository.GetAll(new TaskQuery
        {
            SortBy = TaskSortBy.CreatedAt,
            SortDescending = true
        });
        var urgent = tasks
            .Where(t => t.Status != TaskStatus.Completed &&
                (t.Priority >= TaskPriority.High || t.DueDate <= now.AddDays(1)))
            .ToList();

        return new TaskOverview(
            tasks,
            TaskSorter.SortByUrgency(urgent),
            tasks.Where(t => t.Status == TaskStatus.New)
            .ToList()
        );
    }
}
