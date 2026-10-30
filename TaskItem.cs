using System.Dynamic;

namespace Chronify
{
    public class TaskItem
    {
        public string Title {get; private set;} = string.Empty;
        public string Description {get; private set;} = string.Empty;
        public DateTime DueDate {get; set;}
        public TaskPriority Priority {get; private set;} = TaskPriority.Medium;
        public TaskStatus Status {get; private set;} = TaskStatus.InProgress;
        public Guid Id { get; init; } = Guid.NewGuid();
        public bool IsOverdue => Status != TaskStatus.Done && DateTime.Now > DueDate;
        public DateTime CreatedAt { get; init; } = DateTime.Now;
        public TimeSpan TimeRemaining => DueDate - DateTime.Now;

        public TaskItem(string title, string description, DateTime dueDate, TaskPriority priority, TaskStatus status)
        {
            Title = title;
            Description = description;
            DueDate = dueDate;
            Priority = priority;
            Status = status;
        }
        public TaskItem()
        {}
        public void CompleteTask()
        {
            Status = TaskStatus.Done;
        }
        public void SetPriority(TaskPriority newpriority)
        {
            Priority = newpriority;
        }
        public void UpdateDetails(string title, string description)
        {
            Title = title;
            Description = description;
        }
        public void ChangeDueDate(DateTime newDate)
        {
            DueDate = newDate;
        }
    }   
}