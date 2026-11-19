using Microsoft.EntityFrameworkCore;

namespace Chronify;

public class UserPreferencesRepository(Guid userId, Func<AppDbContext>? createContext = null)
{
    private readonly Func<AppDbContext> _createContext = createContext ?? (() => new AppDbContext());

    public UserPreferences Get()
    {
        using var context = _createContext();
        var preferences = context.UserPreferences.AsNoTracking().FirstOrDefault(p => p.UserId == userId);
        if (preferences is not null)
        {
            return preferences;
        }

        preferences = new UserPreferences { UserId = userId };
        context.UserPreferences.Add(preferences);
        context.SaveChanges();
        return preferences;
    }

    public void Save(int dailyLimit, int planLimit)
    {
        if (dailyLimit is < 1 or > 1440 || planLimit is < 1 or > 1440)
        {
            throw new ArgumentException("Ліміти часу: від 1 до 1440 хвилин.");
        }

        using var context = _createContext();
        var preferences = context.UserPreferences.FirstOrDefault(p => p.UserId == userId);
        if (preferences is null)
        {
            preferences = new UserPreferences { UserId = userId };
            context.UserPreferences.Add(preferences);
        }
        preferences.DailyLimitMinutes = dailyLimit;
        preferences.PlanLimitMinutes = planLimit;
        context.SaveChanges();
    }
}
