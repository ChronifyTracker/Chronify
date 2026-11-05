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