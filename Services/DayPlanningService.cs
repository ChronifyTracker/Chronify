namespace Chronify.Services;

public record DayPlan(
    List<TaskItem> Tasks,
    int UsedMinutes,
    int LimitMinutes,
    int WithoutEstimate
);

public class DayPlanningService
{
    public DayPlan Build(
        IEnumerable<TaskItem> tasks,
        int availableMinutes,
        DateTime now
    )
    {
        if (availableMinutes < 1 || availableMinutes > 1440)
        {
            throw new ArgumentException(Texts.Get("Errors.PlanLimit"));
        }

        var active = tasks.Where(t => t.Status != TaskStatus.Completed).ToList();
        var ordered = active.Where(t => t.EstimatedMinutes > 0)
            .OrderByDescending(t => DynamicPriorityCalculator.Calculate(t, now))
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue);
        var selected = new List<TaskItem>();
        int used = 0;
        // Жадібний алгоритм: беремо найважливішу задачу, якщо вона вміщується в залишок часу.
        foreach (var task in ordered)
        {
            if (task.EstimatedMinutes <= availableMinutes - used)
            {
                selected.Add(task);
                used += task.EstimatedMinutes;
            }
        }
        return new DayPlan(
            selected,
            used,
            availableMinutes,
            active.Count(t => t.EstimatedMinutes == 0)
        );
    }
}
