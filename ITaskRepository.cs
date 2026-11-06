namespace Chronify
{
    public interface ITaskRepository
    {
        void Add(TaskItem task);
        TaskItem? GetById(Guid taskId);
        List<TaskItem> GetAll(TaskQuery? query = null);
        bool Update(TaskItem task);
        bool Delete(Guid taskId);
    }
}