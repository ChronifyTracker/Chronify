using System.Dynamic;

namespace Chronify
{
    public class TaskItem(string title, string description, DateTime? dueDate, TaskPriority priority, TaskStatus status)
    {
        public string Title { get; private set; } = title;
        public string Description { get; private set; } = description;
        public DateTime? DueDate { get; set; } = dueDate;
        public TaskPriority Priority { get; private set; } = priority;
        public TaskStatus Status { get; private set; } = status;
        public Guid Id { get; init; } = Guid.NewGuid();
        public bool IsOverdue =>
            Status != TaskStatus.Completed &&
            DueDate.HasValue &&
            DateTime.Now > DueDate.Value;
        public DateTime CreatedAt { get; init; } = DateTime.Now;
        public TimeSpan? TimeRemaining =>
            DueDate.HasValue ? DueDate.Value - DateTime.Now : null;

        public void CompleteTask()
        {
            Status = TaskStatus.Completed;
        }
        public void SetPriority(TaskPriority newPriority)
        {
            Priority = newPriority;
        }
        public void UpdateDetails(string title, string description)
        {
            Title = title;
            Description = description;
        }
        public void ChangeDueDate(DateTime? newDate)
        {
            DueDate = newDate;
        }
    }
}