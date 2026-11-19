namespace Chronify.Services;

public record CategoryStatistics(
    string Category,
    int TaskCount,
    int PlannedMinutes,
    int ActualMinutes
);
public record StatisticsResult(
    int Active,
    int Overdue,
    int Completed,
    string OnTimeRate,
    IReadOnlyList<CategoryStatistics> Categories
);

public class StatisticsService
{
    public StatisticsResult Calculate(
        List<TaskItem> tasks,
        List<WorkSession> sessions,
        DateTime from,
        DateTime to,
        DateTime now
    )
    {
        // Межі періоду: початок включно, кінець не включно.
        var periodTasks = tasks.Where(t => (t.DueDate ?? t.CreatedAt) >= from
            && (t.DueDate ?? t.CreatedAt) < to).ToList();
        var completed = tasks.Where(t => t.CompletedAt >= from && t.CompletedAt < to).ToList();
        var withDeadline = completed.Where(t => t.DueDate.HasValue).ToList();
        int onTime = withDeadline.Count(t => t.CompletedAt <= t.DueDate);
        string rate = withDeadline.Count == 0 ? "Нет данных" : $"{100.0 * onTime / withDeadline.Count:0.#}%";
        var periodSessions = sessions.Where(s => s.StartedAt >= from && s.StartedAt < to).ToList();
        var names = tasks.Select(t => t.Category?.Name ?? "Без категории").Distinct().OrderBy(n => n);
        var rows = new List<CategoryStatistics>();
        foreach (string name in names)
        {
            var group = periodTasks.Where(t => (t.Category?.Name ?? "Без категории") == name).ToList();
            var taskIds = tasks.Where(t => (t.Category?.Name ?? "Без категории") == name).Select(t => t.Id)
                .ToHashSet();
            int actual = periodSessions.Where(s => taskIds.Contains(s.TaskId)).Sum(s => s.Minutes);
            if (group.Count > 0 || actual > 0)
            {
                rows.Add(new CategoryStatistics(
                    name,
                    group.Count,
                    group.Sum(t => t.EstimatedMinutes),
                    actual
                ));
            }
        }
        return new StatisticsResult(
            periodTasks.Count(t => t.Status != TaskStatus.Completed),
            periodTasks.Count(t => t.Status != TaskStatus.Completed && t.DueDate < now),
            completed.Count,
            rate,
            rows
        );
    }
}
