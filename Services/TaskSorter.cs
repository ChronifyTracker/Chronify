namespace Chronify.Services;

public static class TaskSorter
{
    private static readonly TimeSpan PriorityHeadStart = TimeSpan.FromHours(24);

    public static List<TaskItem> SortByUrgency(IEnumerable<TaskItem> tasks)
    {
        return tasks
            .OrderBy(task => task.Status == TaskStatus.Completed)
            .ThenBy(StartByTicks)
            .ThenByDescending(task => task.Priority)
            .ToList();
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
