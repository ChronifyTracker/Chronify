namespace Chronify
{
    public interface ITaskRepository
    {
        void Add(TaskItem task);
        TaskItem? GetById(Guid taskId);
        List<TaskItem> GetAll();
        List<TaskItem> GetAll(TaskQuery? query)
        {
            return Services.TaskFilteringService.Apply(GetAll(), query, DateTime.Now);
        }

        bool Update(TaskItem task);
        bool Delete(Guid taskId);
    }
}
