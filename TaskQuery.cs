namespace Chronify
{
    public class TaskQuery
    {
        public string? Search { get; init; }
        public TaskPriority? Priority { get; init; }
        public Guid? CategoryId { get; init; }
        public TaskStatus? Status { get; init; }
        public TaskSortBy? SortBy { get; init; }
        public bool SortDescending { get; init; }
    }
}
