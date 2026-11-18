namespace Chronify;

public class FilterPreset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public string? Search { get; set; }
    public TaskPriority? Priority { get; set; }
    public Guid? CategoryId { get; set; }
    public TaskStatus? Status { get; set; }
    public TaskDateFilter DateFilter { get; set; }
    public TaskSortBy? SortBy { get; set; }
    public bool SortDescending { get; set; }

    public TaskQuery ToQuery()
    {
        return new()
        {
            Search = Search,
            Priority = Priority,
            CategoryId = CategoryId,
            Status = Status,
            DateFilter = DateFilter,
            SortBy = SortBy,
            SortDescending = SortDescending
        };
    }
}
