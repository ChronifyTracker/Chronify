namespace Chronify
{
    public interface ITaskRepository
    {
        void Add(TaskItem task);
        TaskItem? GetById(Guid taskId);
        List<TaskItem> GetAll();
        List<TaskItem> Search(string query);
        bool Update(TaskItem task);
        bool Delete(Guid taskId);
    }
}