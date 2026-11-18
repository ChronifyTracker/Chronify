namespace Chronify
{
    public class TaskItem
    {
        public const int MinEstimatedMinutes = 0, MaxEstimatedMinutes = 100_000;

        public Guid Id { get; init; } = Guid.NewGuid();
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public DateTime? DueDate { get; private set; }
        public int EstimatedMinutes { get; private set; }
        public TaskPriority Priority { get; private set; } = TaskPriority.Medium;
        public TaskStatus Status { get; private set; } = TaskStatus.New;
        public DateTime CreatedAt { get; init; } = DateTime.Now;
        public DateTime? CompletedAt { get; private set; }

        public Guid? CategoryId { get; private set; }
        public Category? Category { get; private set; }

        public bool IsOverdue => Status != TaskStatus.Completed && DueDate is DateTime dueDate && DateTime.Now > dueDate;

        public bool IsDueSoon
        {
            get
            {
                return Status != TaskStatus.Completed && TimeRemaining is TimeSpan left && left >= TimeSpan.Zero
                    && left <= TimeSpan.FromDays(1);
            }
        }

        public bool IsUrgent
        {
            get
            {
                return Status != TaskStatus.Completed && (Priority >= TaskPriority.High || IsOverdue || IsDueSoon);
            }
        }

        public TimeSpan? TimeRemaining => DueDate is DateTime dueDate ? dueDate - DateTime.Now : null;

        public TaskItem(
            string title,
            string description = "",
            DateTime? dueDate = null,
            int estimatedMinutes = 0,
            TaskPriority priority = TaskPriority.Medium,
            TaskStatus status = TaskStatus.New,
            Category? category = null
        )
        {
            SetTitle(title);
            SetDescription(description);
            SetDueDate(dueDate);
            SetEstimatedMinutes(estimatedMinutes);
            SetPriority(priority);
            SetCategory(category);
            SetStatus(status);
        }

        private TaskItem()
        {
        }

        // Зовнішнє завдання може вже мати прострочений дедлайн. Ручне введення й далі перевіряється.
        public static TaskItem FromImport(
            string title,
            string description = "",
            DateTime? dueDate = null,
            int estimatedMinutes = 0,
            TaskPriority priority = TaskPriority.Medium,
            TaskStatus status = TaskStatus.New,
            Category? category = null,
            DateTime? completedAt = null,
            DateTime? createdAt = null
        )
        {
            var task = new TaskItem(
                title,
                description,
                estimatedMinutes: estimatedMinutes,
                priority: priority,
                category: category
            )
            { CreatedAt = createdAt ?? DateTime.Now };
            task.DueDate = dueDate;
            task.SetStatus(status);
            if (status == TaskStatus.Completed && completedAt is not null)
            {
                task.CompletedAt = completedAt;
            }

            return task;
        }

        public void StartTask()
        {
            EnsureNotCompleted();
            SetStatus(TaskStatus.InProgress);
        }

        public void CompleteTask()
        {
            EnsureNotCompleted();
            SetStatus(TaskStatus.Completed);
        }

        public void ReopenTask()
        {
            SetStatus(TaskStatus.InProgress);
        }

        private void SetStatus(TaskStatus status)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ModelValidationException(ModelError.TaskStatusInvalid);
            }

            Status = status;

            if (status == TaskStatus.Completed)
            {
                CompletedAt = DateTime.Now;
            }
            else
            {
                CompletedAt = null;
            }
        }

        public void SetPriority(TaskPriority newPriority)
        {
            EnsureNotCompleted();

            if (!Enum.IsDefined(newPriority))
            {
                throw new ModelValidationException(ModelError.TaskPriorityInvalid);
            }

            Priority = newPriority;
        }

        public void SetCategory(Category? category)
        {
            EnsureNotCompleted();

            Category = category;
            CategoryId = category?.Id;
        }

        public void SetEstimatedMinutes(int minutes)
        {
            EnsureNotCompleted();

            if (minutes < MinEstimatedMinutes || minutes > MaxEstimatedMinutes)
            {
                throw new ModelValidationException(ModelError.EstimatedMinutesOutOfRange);
            }

            EstimatedMinutes = minutes;
        }

        public void SetTitle(string title)
        {
            EnsureNotCompleted();

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ModelValidationException(ModelError.TaskTitleRequired);
            }

            Title = title.Trim();
        }

        public void SetDescription(string description)
        {
            EnsureNotCompleted();
            ArgumentNullException.ThrowIfNull(description);

            Description = description.Trim();
        }

        public void SetDueDate(DateTime? dueDate)
        {
            EnsureNotCompleted();

            // Прострочений дедлайн можна залишити під час редагування інших полів.
            if (dueDate < DateTime.Now && dueDate != DueDate)
            {
                throw new ModelValidationException(ModelError.DueDateInPast);
            }

            DueDate = dueDate;
        }

        private void EnsureNotCompleted()
        {
            if (Status == TaskStatus.Completed)
            {
                throw new ModelValidationException(ModelError.TaskCompleted);
            }
        }

        // Редагуємо копію: помилка форми не повинна змінювати початковий об'єкт.
        public TaskItem Copy()
        {
            return new()
            {
                Id = Id,
                Title = Title,
                Description = Description,
                DueDate = DueDate,
                EstimatedMinutes = EstimatedMinutes,
                Priority = Priority,
                Status = Status,
                CreatedAt = CreatedAt,
                CompletedAt = CompletedAt,
                CategoryId = CategoryId,
                Category = Category
            };
        }
    }
}
