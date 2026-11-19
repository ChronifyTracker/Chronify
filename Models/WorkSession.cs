namespace Chronify;

public class WorkSession
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid TaskId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public int Minutes { get; private set; }
    public string Note { get; private set; } = "";

    private WorkSession()
    {
    }

    public WorkSession(
        Guid userId,
        Guid taskId,
        DateTime startedAt,
        int minutes,
        string? note
    )
    {
        if (minutes < 1 || minutes > 1440)
        {
            throw new ArgumentException("Тривалість сесії: від 1 до 1440 хвилин.");
        }

        if (startedAt > DateTime.Now)
        {
            throw new ArgumentException("Не можна записати роботу в майбутньому.");
        }

        UserId = userId;
        TaskId = taskId;
        StartedAt = startedAt;
        Minutes = minutes;
        Note = note?.Trim() ?? "";
    }
}
