using Microsoft.EntityFrameworkCore;

namespace Chronify
{
    public class SqliteTaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;

        public SqliteTaskRepository()
        {
            _context = new AppDbContext();
        }

        public void Add(TaskItem task)
        {
            _context.Tasks.Add(task);
            _context.SaveChanges();
        }

        public TaskItem? GetById(Guid taskId)
        {
            return _context.Tasks
                .Include(t => t.Category)
                .FirstOrDefault(t => t.Id == taskId);
        }

        public List<TaskItem> GetAll()
        {
            return _context.Tasks
                .Include(t => t.Category)
                .ToList();
        }

        public bool Update(TaskItem task)
        {
            if (!_context.Tasks.Any(t => t.Id == task.Id))
                return false;

            _context.Tasks.Update(task);
            _context.SaveChanges();
            return true;
        }

        public bool Delete(Guid taskId)
        {
            var task = _context.Tasks.Find(taskId);

            if (task is null)
                return false;

            _context.Tasks.Remove(task);
            _context.SaveChanges();
            return true;
        }
    }
}
