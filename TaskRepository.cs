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

        public List<TaskItem> GetAll(TaskFilter? filter = null)
        {
            IEnumerable<TaskItem> tasks = _tasks;

            if (!string.IsNullOrWhiteSpace(filter?.SearchQuery))
            {
                var searchQuery = filter.SearchQuery.Trim();
                tasks = tasks.Where(t => t.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    || t.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (filter?.Priority is TaskPriority priority)
                tasks = tasks.Where(t => t.Priority == priority);

            if (filter?.CategoryId is Guid categoryId)
                tasks = tasks.Where(t => t.CategoryId == categoryId);

            if (filter?.Status is TaskStatus status)
                tasks = tasks.Where(t => t.Status == status);

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
