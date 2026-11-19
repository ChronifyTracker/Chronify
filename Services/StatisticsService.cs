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
        string rate = withDeadline.Count == 0
            ? Texts.Get("Common.NoData")
            : (100.0 * onTime / withDeadline.Count).ToString("0.#", LocalizationService.Current.Culture) + "%";
        var periodSessions = sessions.Where(s => s.StartedAt >= from && s.StartedAt < to).ToList();
        // Групуємо за ID, а не за перекладеною назвою «Без категорії».
        var categories = tasks.GroupBy(task => task.CategoryId)
            .Select(group => new
            {
                Id = group.Key,
                Name = group.First().Category?.Name ?? Texts.Get("Categories.None"),
                TaskIds = group.Select(task => task.Id).ToHashSet()
            })
            .OrderBy(category => category.Name);
        var rows = new List<CategoryStatistics>();
        foreach (var category in categories)
        {
            var group = periodTasks.Where(task => task.CategoryId == category.Id).ToList();
            int actual = periodSessions.Where(session => category.TaskIds.Contains(session.TaskId)).Sum(session => session.Minutes);
            if (group.Count > 0 || actual > 0)
            {
                rows.Add(new CategoryStatistics(
                    category.Name,
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
