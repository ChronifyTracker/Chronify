namespace Chronify
{
    public class TaskItem
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public DateTime? DueDate { get; private set; }
        public int EstimatedMinutes { get; private set; }
        public TaskPriority Priority { get; private set; } = TaskPriority.Medium;
        public TaskStatus Status { get; private set; } = TaskStatus.New;
        public DateTime CreatedAt { get; init; } = DateTime.Now;

        public Guid? CategoryId { get; private set; }
        public Category? Category { get; private set; }

        public bool IsOverdue => Status != TaskStatus.Completed && DueDate is DateTime dueDate && DateTime.Now > dueDate;
        public TimeSpan? TimeRemaining => DueDate is DateTime dueDate ? dueDate - DateTime.Now : null;

        public TaskItem(
            string title,
            string description = "",
            DateTime? dueDate = null,
            int estimatedMinutes = 0,
            TaskPriority priority = TaskPriority.Medium,
            TaskStatus status = TaskStatus.New,
            Category? category = null)
        {
            SetTitle(title);
            SetDescription(description);
            SetDueDate(dueDate);
            SetEstimatedMinutes(estimatedMinutes);
            SetPriority(priority);
            SetStatus(status);

            if (category is not null)
                SetCategory(category);
        }

        private TaskItem() { }

        public void StartTask()
        {
            SetStatus(TaskStatus.InProgress);
        }

        public void CompleteTask()
        {
            SetStatus(TaskStatus.Completed);
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
            if (category is null)
                throw new ArgumentNullException(nameof(category), "Категорію не вказано.");

            Category = category;
            CategoryId = category.Id;
        }

        public void SetEstimatedMinutes(int minutes)
        {
            if (minutes < 0)
                throw new ArgumentOutOfRangeException(nameof(minutes), "Орієнтовна тривалість не може бути від'ємною.");

            EstimatedMinutes = minutes;
        }

        public void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Назва завдання обов'язкова.", nameof(title));

            Title = title.Trim();
        }

        public void SetDescription(string description)
        {
            if (description is null)
                throw new ArgumentNullException(nameof(description), "Опис не вказано.");

            Description = description.Trim();
        }

        public void SetDueDate(DateTime? dueDate)
        {
            DueDate = dueDate;
        }
    }
}
