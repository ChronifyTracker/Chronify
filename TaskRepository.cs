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

            if (filter?.Priority is TaskPriority priority)
                tasks = tasks.Where(t => t.Priority == priority);

            if (filter?.CategoryId is Guid categoryId)
                tasks = tasks.Where(t => t.CategoryId == categoryId);

            if (filter?.Status is TaskStatus status)
                tasks = tasks.Where(t => t.Status == status);

            return tasks.ToList();
        }

        public List<TaskItem> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return GetAll();

            query = query.Trim();

            return _tasks
                .Where(t => t.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || t.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
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
