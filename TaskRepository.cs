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

        public List<TaskItem> GetAll()
        {
            return _tasks.ToList();
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
