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

        public List<TaskItem> GetAll(TaskQuery? query = null)
        {
            IQueryable<TaskItem> tasks = _context.Tasks
                .Include(t => t.Category);

            if (query?.Priority is TaskPriority priority)
                tasks = tasks.Where(t => t.Priority == priority);

            if (query?.CategoryId is Guid categoryId)
                tasks = tasks.Where(t => t.CategoryId == categoryId);

            if (query?.Status is TaskStatus status)
                tasks = tasks.Where(t => t.Status == status);

            if (query?.SortBy is TaskSortBy sortBy)
            {
                var sortDescending = query.SortDescending;
                tasks = sortBy switch
                {
                    TaskSortBy.DueDate => sortDescending
                        ? tasks.OrderByDescending(t => t.DueDate)
                        : tasks.OrderBy(t => t.DueDate),
                    TaskSortBy.Priority => sortDescending
                        ? tasks.OrderByDescending(t => t.Priority)
                        : tasks.OrderBy(t => t.Priority),
                    TaskSortBy.CreatedAt => sortDescending
                        ? tasks.OrderByDescending(t => t.CreatedAt)
                        : tasks.OrderBy(t => t.CreatedAt),
                    TaskSortBy.Title => sortDescending
                        ? tasks.OrderByDescending(t => t.Title)
                        : tasks.OrderBy(t => t.Title),
                    _ => tasks
                };
            }

            var results = tasks.ToList();
            if (string.IsNullOrWhiteSpace(query?.Search))
                return results;

            var searchQuery = query.Search.Trim();
            return results
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
