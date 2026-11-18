namespace Chronify;

public class TaskAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TaskId { get; set; }
    public string Name { get; set; } = "";
    public byte[] Content { get; set; } = [];
}
