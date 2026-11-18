namespace Chronify.Services;

public record WorkloadDay(
    DateTime Date,
    int TaskCount,
    int Minutes,
    bool IsOverloaded
);

public class WorkloadService
{
    public List<WorkloadDay> BuildMonth(
        IEnumerable<TaskItem> tasks,
        DateTime month,
        int dailyLimit
    )
    {
        var active = tasks.Where(t => t.Status != TaskStatus.Completed && t.DueDate.HasValue).ToList();
        var first = new DateTime(month.Year, month.Month, 1);
        var result = new List<WorkloadDay>();
        for (int i = 0; i < DateTime.DaysInMonth(month.Year, month.Month); i++)
        {
            DateTime date = first.AddDays(i);
            var dayTasks = active.Where(t => t.DueDate!.Value.Date == date).ToList();
            int minutes = dayTasks.Sum(t => t.EstimatedMinutes);
            result.Add(new WorkloadDay(
                date,
                dayTasks.Count,
                minutes,
                minutes > dailyLimit
            ));
        }
        return result;
    }
}
