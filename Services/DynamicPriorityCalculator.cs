namespace Chronify.Services;

public static class DynamicPriorityCalculator
{
    public static int Calculate(TaskItem task, DateTime now)
    {
        if (task.Status == TaskStatus.Completed)
        {
            return 0;
        }

        // Проста навчальна формула: важливість + терміновість + обсяг роботи.
        int importance = (int)task.Priority * 25;
        double days = task.DueDate is DateTime due ? (due - now).TotalDays : double.MaxValue;
        int urgency = days < 0 ? 100 : days <= 1 ? 80 : days <= 3 ? 50 : days <= 7 ? 20 : 0;
        int effort = Math.Min(40, task.EstimatedMinutes / 15);
        return importance + urgency + effort;
    }
}
