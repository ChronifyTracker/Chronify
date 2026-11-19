using Chronify.Services;
using Microsoft.EntityFrameworkCore;

namespace Chronify;

public class SqliteTaskRepository : ITaskRepository
{
    private readonly Guid? _userId;
    private readonly Func<AppDbContext> _createContext;

    public SqliteTaskRepository(Guid? userId = null, Func<AppDbContext>? createContext = null)
    {
        _userId = userId;
        _createContext = createContext ?? (() => new AppDbContext());
    }

    public void Add(TaskItem task)
    {
        using var context = _createContext();
        CheckCategory(context, task.CategoryId);
        if (task.UserId is not null && task.UserId != _userId)
        {
            throw new InvalidOperationException("Немає доступу до цієї задачі.");
        }

        task.UserId = _userId;
        if (task.Category is not null)
        {
            context.Attach(task.Category);
        }

        context.Tasks.Add(task);
        context.SaveChanges();
    }

    public TaskItem? GetById(Guid taskId)
    {
        using var context = _createContext();
        return context.Tasks.Include(t => t.Category).AsNoTracking()
            .FirstOrDefault(t => t.Id == taskId && t.UserId == _userId);
    }

    public List<TaskItem> GetAll()
    {
        return GetAll(null);
    }

    public List<TaskItem> GetAll(TaskQuery? query)
    {
        using var context = _createContext();
        var tasks = context.Tasks.Include(t => t.Category).AsNoTracking()
            .Where(t => t.UserId == _userId).ToList();
        return TaskFilteringService.Apply(tasks, query, DateTime.Now);
    }

    public bool Update(TaskItem task)
    {
        using var context = _createContext();
        var stored = context.Tasks.FirstOrDefault(t => t.Id == task.Id && t.UserId == _userId);
        if (stored is null || task.UserId != _userId)
        {
            return false;
        }

        CheckCategory(context, task.CategoryId);
        if (stored.GroupAssignmentId is Guid assignmentId)
        {
            if (stored.Status != TaskStatus.InProgress || task.Status != TaskStatus.Completed
                || stored.GroupAssignmentId != task.GroupAssignmentId || stored.AssignedByName != task.AssignedByName
                || stored.Title != task.Title || stored.Description != task.Description || stored.Priority != task.Priority
                || stored.DueDate != task.DueDate || stored.CategoryId != task.CategoryId || stored.EstimatedMinutes != task.EstimatedMinutes)
            {
                throw new InvalidOperationException("Условия групповой задачи задаёт постановщик. Исполнитель может завершить начатую задачу.");
            }

            var cached = context.CachedAssignments.Single(a => a.UserId == _userId && a.Id == assignmentId);
            cached.PendingCompletion = true;
            cached.Status = 2;
        }
        context.Entry(stored).CurrentValues.SetValues(task);
        context.SaveChanges();
        return true;
    }

    public bool Delete(Guid taskId)
    {
        using var context = _createContext();
        var task = context.Tasks.FirstOrDefault(t => t.Id == taskId && t.UserId == _userId);
        if (task is null)
        {
            return false;
        }

        if (task.GroupAssignmentId is not null)
        {
            throw new InvalidOperationException("Назначенную задачу нельзя удалить из личного списка.");
        }

        context.Tasks.Remove(task);
        context.SaveChanges();
        return true;
    }

    private void CheckCategory(AppDbContext context, Guid? categoryId)
    {
        if (categoryId is Guid id && !context.Categories.Any(c => c.Id == id && c.UserId == _userId))
        {
            throw new InvalidOperationException("Категорія не належить поточному користувачу.");
        }
    }
}
