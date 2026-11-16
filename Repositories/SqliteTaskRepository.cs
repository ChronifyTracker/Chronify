using Microsoft.EntityFrameworkCore;

namespace Chronify
{
    public class SqliteTaskRepository : ITaskRepository
    {
        public void Add(TaskItem task)
        {
            using var context = new AppDbContext();

            if (task.Category is not null)
                context.Attach(task.Category);

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

        public List<TaskItem> GetAll(TaskQuery? query = null)
        {
            using var context = new AppDbContext();

            IQueryable<TaskItem> tasks = context.Tasks
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
            using var context = new AppDbContext();

            if (!context.Tasks.Any(t => t.Id == task.Id))
                return false;

            if (task.Category is not null)
                context.Attach(task.Category);

            context.Tasks.Update(task);
            context.SaveChanges();
            return true;
        }

        public bool Delete(Guid taskId)
        {
            using var context = new AppDbContext();

            var task = context.Tasks.Find(taskId);

            if (task is null)
                return false;

            context.Tasks.Remove(task);
            context.SaveChanges();
            return true;
        }
    }
}
