namespace Chronify
{
    public class TaskItem
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int EstimatedMinutes { get; private set; }
        public TaskPriority Priority { get; private set; } = TaskPriority.Medium;
        public TaskStatus Status { get; private set; } = TaskStatus.New;
        public DateTime CreatedAt { get; init; } = DateTime.Now;

        public Guid? CategoryId { get; private set; }
        public Category? Category { get; private set; }

        public bool IsOverdue => Status != TaskStatus.Done && DateTime.Now > DueDate;
        public TimeSpan TimeRemaining => DueDate - DateTime.Now;

        public TaskItem(
            string title, 
            string description, 
            DateTime dueDate, 
            TaskPriority priority, 
            TaskStatus status = TaskStatus.New,
            int estimatedMinutes = 0,
            Category? category = null)
        {
            Title = title;
            Description = description;
            DueDate = dueDate;
            Priority = priority;
            Status = status;
            EstimatedMinutes = estimatedMinutes;

            if (category != null)
            {
                Category = category;
                CategoryId = category.Id;
            }
        }

        public TaskItem()
        {
        }

        public void StartTask()
        {
            Status = TaskStatus.InProgress;
        }

        public void CompleteTask()
        {
            Status = TaskStatus.Done;
        }

        public void SetStatus(TaskStatus status)
        {
            Status = status;
        }

        public void SetPriority(TaskPriority newPriority)
        {
            Priority = newPriority;
        }

        public void SetCategory(Category category)
        {
            Category = category;
            CategoryId = category.Id;
        }

        public void SetEstimatedMinutes(int minutes)
        {
            EstimatedMinutes = minutes;
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
