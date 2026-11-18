namespace Chronify.Services;

public static class TaskSorter
{
    private static readonly TimeSpan PriorityHeadStart = TimeSpan.FromHours(24);

    public static List<TaskItem> SortByUrgency(IEnumerable<TaskItem> tasks)
    {
        return Sort(tasks, TaskSortOrder.Urgency);
    }

    public static List<TaskItem> Sort(IEnumerable<TaskItem> tasks, TaskSortOrder order)
    {
        var openFirst = tasks.OrderBy(task => task.Status == TaskStatus.Completed);

        var sorted = order switch
        {
            TaskSortOrder.DueDate => openFirst
                .ThenBy(task => task.DueDate ?? DateTime.MaxValue),
            TaskSortOrder.Priority => openFirst
                .ThenByDescending(task => task.Priority)
                .ThenBy(StartByTicks),
            TaskSortOrder.Category => openFirst
                .ThenBy(task => task.Category is null)
                .ThenBy(task => task.Category?.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(StartByTicks),
            TaskSortOrder.Title => openFirst
                .ThenBy(task => task.Title, StringComparer.CurrentCultureIgnoreCase),
            TaskSortOrder.Newest => openFirst
                .ThenByDescending(task => task.CreatedAt),
            _ => openFirst
                .ThenBy(StartByTicks)
                .ThenByDescending(task => task.Priority)
        };

        return sorted.ToList();
    }

    private static long StartByTicks(TaskItem task)
    {
        if (task.DueDate is not DateTime dueDate)
            return long.MaxValue;

        var workTime = TimeSpan.FromMinutes(task.EstimatedMinutes);
        var headStart = PriorityHeadStart * (int)task.Priority;

        return dueDate.Ticks - workTime.Ticks - headStart.Ticks;
    }
}
