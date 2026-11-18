namespace Chronify.Services;

public static class TaskFilteringService
{
    public static List<TaskItem> Apply(
        IEnumerable<TaskItem> tasks,
        TaskQuery? query,
        DateTime now
    )
    {
        if (query is null)
        {
            return tasks.ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string text = query.Search.Trim();
            tasks = tasks.Where(t => t.Title.Contains(text, StringComparison.OrdinalIgnoreCase)
                || t.Description.Contains(text, StringComparison.OrdinalIgnoreCase));
        }
        if (query.CategoryId is Guid category)
        {
            tasks = tasks.Where(t => t.CategoryId == category);
        }

        if (query.Priority is TaskPriority priority)
        {
            tasks = tasks.Where(t => t.Priority == priority);
        }

        if (query.Status is TaskStatus status)
        {
            tasks = tasks.Where(t => t.Status == status);
        }

        tasks = query.DateFilter switch
        {
            TaskDateFilter.Today => tasks.Where(t => t.DueDate?.Date == now.Date),
            TaskDateFilter.NextThreeDays => tasks.Where(t => t.DueDate >= now.Date
            && t.DueDate < now.Date.AddDays(3)),
            TaskDateFilter.Overdue => tasks.Where(t => t.Status != TaskStatus.Completed && t.DueDate < now),
            _ => tasks
        };

        tasks = query.SortBy switch
        {
            TaskSortBy.Category => TaskSorter.Sort(tasks, TaskSortOrder.Category),
            TaskSortBy.WeightedUrgency => TaskSorter.Sort(tasks, TaskSortOrder.Urgency),
            TaskSortBy.Edf => tasks.OrderBy(t => t.DueDate ?? DateTime.MaxValue).ThenByDescending(t => t.Priority),
            TaskSortBy.DynamicPriority => tasks.OrderByDescending(t => DynamicPriorityCalculator.Calculate(t, now))
                .ThenBy(t => t.DueDate ?? DateTime.MaxValue),
            TaskSortBy.DueDate => query.SortDescending ? tasks.OrderByDescending(t => t.DueDate) : tasks.OrderBy(t => t.DueDate ?? DateTime.MaxValue),
            TaskSortBy.Priority => query.SortDescending ? tasks.OrderByDescending(t => t.Priority) : tasks.OrderBy(t => t.Priority),
            TaskSortBy.CreatedAt => query.SortDescending ? tasks.OrderByDescending(t => t.CreatedAt) : tasks.OrderBy(t => t.CreatedAt),
            TaskSortBy.Title => query.SortDescending ? tasks.OrderByDescending(t => t.Title) : tasks.OrderBy(t => t.Title),
            _ => tasks
        };
        return tasks.ToList();
    }
}
