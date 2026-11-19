namespace Chronify;

// Зв'язок імпортованої задачі з джерелом для перевірки повторного імпорту.
public class ExternalSourceInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TaskId { get; set; }
    public string Provider { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string Url { get; set; } = "";
    public string SyncState { get; set; } = "Pending";
    public DateTime? LastSyncedAt { get; set; }
}
