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

        public TaskItem? GetById(Guid id)
        {
            return _context.Tasks
                .Include(t => t.Category)
                .FirstOrDefault(t => t.Id == id);
        }

        public List<TaskItem> GetAll()
        {
            return _context.Tasks
                .Include(t => t.Category)
                .ToList();
        }
    }
}
