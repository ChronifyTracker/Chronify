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

        public List<TaskItem> GetAll(TaskFilter? filter = null)
        {
            IQueryable<TaskItem> tasks = _context.Tasks
                .Include(t => t.Category);

            if (filter?.Priority is TaskPriority priority)
                tasks = tasks.Where(t => t.Priority == priority);

            if (filter?.CategoryId is Guid categoryId)
                tasks = tasks.Where(t => t.CategoryId == categoryId);

            if (filter?.Status is TaskStatus status)
                tasks = tasks.Where(t => t.Status == status);

            var filteredTasks = tasks.ToList();
            if (string.IsNullOrWhiteSpace(filter?.SearchQuery))
                return filteredTasks;

            var searchQuery = filter.SearchQuery.Trim();
            return filteredTasks
                .Where(t => t.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
                    || t.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
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
