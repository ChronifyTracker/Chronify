namespace Chronify
{
    public class TaskFilter
    {
        public string? SearchQuery { get; init; }
        public TaskPriority? Priority { get; init; }
        public Guid? CategoryId { get; init; }
        public TaskStatus? Status { get; init; }
    }
}
