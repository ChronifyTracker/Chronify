namespace Chronify
{
    public interface ITaskRepository
    {
        void Add(TaskItem task);
        TaskItem? GetById(Guid id);
        List<TaskItem> GetAll();
    }
}