namespace Chronify
{
    public class TaskRepository : ITaskRepository
    {
        private List<TaskItem> _tasks = new();

        public void Add(TaskItem task)
        {
            _tasks.Add(task);
        }

        public TaskItem? GetById(Guid taskId)
        {
            return _tasks.FirstOrDefault(t => t.Id == taskId);
        }

        public List<TaskItem> GetAll(TaskQuery? query = null)
        {
            IEnumerable<TaskItem> tasks = _tasks;

            if (!string.IsNullOrWhiteSpace(query?.Search))
            {
                var searchQuery = query.Search.Trim();
                tasks = tasks.Where(t => t.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    || t.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (query?.Priority is TaskPriority priority)
                tasks = tasks.Where(t => t.Priority == priority);

            if (query?.CategoryId is Guid categoryId)
                tasks = tasks.Where(t => t.CategoryId == categoryId);

            if (query?.Status is TaskStatus status)
                tasks = tasks.Where(t => t.Status == status);

            if (query?.SortBy is TaskSortBy sortBy)
            {
                var sortDescending = query.SortDescending;
                tasks = sortBy switch
                {
                    TaskSortBy.DueDate => sortDescending
                        ? tasks.OrderByDescending(t => t.DueDate)
                        : tasks.OrderBy(t => t.DueDate),
                    TaskSortBy.Priority => sortDescending
                        ? tasks.OrderByDescending(t => t.Priority)
                        : tasks.OrderBy(t => t.Priority),
                    TaskSortBy.CreatedAt => sortDescending
                        ? tasks.OrderByDescending(t => t.CreatedAt)
                        : tasks.OrderBy(t => t.CreatedAt),
                    TaskSortBy.Title => sortDescending
                        ? tasks.OrderByDescending(t => t.Title)
                        : tasks.OrderBy(t => t.Title),
                    _ => tasks
                };
            }

            return tasks.ToList();
        }

        public bool Update(TaskItem task)
        {
            var existingTask = GetById(task.Id);

            if (existingTask is null)
                return false;

            var index = _tasks.IndexOf(existingTask);
            _tasks[index] = task;
            return true;
        }

        public bool Delete(Guid id)
        {
            var task = GetById(id);

            if (task is null)
                return false;

            _tasks.Remove(task);
            return true;
        }
    }
}
