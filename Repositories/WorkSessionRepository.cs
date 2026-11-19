using Microsoft.EntityFrameworkCore;

namespace Chronify;

public class WorkSessionRepository(Guid userId, Func<AppDbContext>? createContext = null)
{
    private readonly Func<AppDbContext> _createContext = createContext ?? (() => new AppDbContext());

    public List<WorkSession> GetAll()
    {
        using var context = _createContext();
        return context.WorkSessions.AsNoTracking().Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartedAt).ToList();
    }

    public void Add(WorkSession session)
    {
        using var context = _createContext();
        if (session.UserId != userId || !context.Tasks.Any(t => t.Id == session.TaskId && t.UserId == userId))
        {
            throw new InvalidOperationException("Немає доступу до задачі.");
        }

        context.WorkSessions.Add(session);
        context.SaveChanges();
    }

    public void Delete(Guid id)
    {
        using var context = _createContext();
        var session = context.WorkSessions.FirstOrDefault(s => s.Id == id && s.UserId == userId);
        if (session is not null)
        {
            context.WorkSessions.Remove(session);
            context.SaveChanges();
        }
    }
}
