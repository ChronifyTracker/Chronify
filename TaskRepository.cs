namespace Chronify
{
    public class TaskRepository : ITaskRepository
    {
        private List<TaskItem> _tasks = new();

        public void Add(TaskItem task)
        {
            _tasks.Add(task);
        }

        public TaskItem? GetById(Guid inputId)
        {
            return _tasks.FirstOrDefault(x => x.Id == inputId);
        }

        public List<TaskItem> GetAll()
        {
            return _tasks.ToList();
        }
    }
}