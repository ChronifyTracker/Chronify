using Microsoft.EntityFrameworkCore;

namespace Chronify
{
    public class SqliteTaskRepository : ITaskRepository
    {
        public void Add(TaskItem task)
        {
            using var context = new AppDbContext();

            if (task.Category is not null)
            {
                context.Attach(task.Category);
            }

            context.Tasks.Add(task);
            context.SaveChanges();
        }

        public TaskItem? GetById(Guid taskId)
        {
            using var context = new AppDbContext();

            return context.Tasks
                .Include(t => t.Category)
                .FirstOrDefault(t => t.Id == taskId);
        }

        public List<TaskItem> GetAll()
        {
            using var context = new AppDbContext();

            return context.Tasks
                .Include(t => t.Category)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }

        public bool Update(TaskItem task)
        {
            using var context = new AppDbContext();

            if (!context.Tasks.Any(t => t.Id == task.Id))
            {
                return false;
            }

            if (task.Category is not null)
            {
                context.Attach(task.Category);
            }

            context.Tasks.Update(task);
            context.SaveChanges();
            return true;
        }

        public bool Delete(Guid taskId)
        {
            using var context = new AppDbContext();

            var task = context.Tasks.Find(taskId);

            if (task is null)
            {
                return false;
            }

            context.Tasks.Remove(task);
            context.SaveChanges();
            return true;
        }
    }
}
