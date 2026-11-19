using Microsoft.EntityFrameworkCore;

namespace Chronify;

public class FilterPresetRepository(Guid userId, Func<AppDbContext>? createContext = null)
{
    private readonly Func<AppDbContext> _createContext = createContext ?? (() => new AppDbContext());

    public List<FilterPreset> GetAll()
    {
        using var context = _createContext();
        return context.FilterPresets.AsNoTracking().Where(p => p.UserId == userId).OrderBy(p => p.Name).ToList();
    }

    public void Save(string name, TaskQuery query)
    {
        name = name.Trim();
        if (name.Length is < 1 or > 50)
        {
            throw new ArgumentException("Назва фільтра: від 1 до 50 символів.");
        }

        using var context = _createContext();
        using var transaction = context.Database.BeginTransaction();
        if (query.CategoryId is Guid categoryId
            && !context.Categories.Any(c => c.Id == categoryId
            && c.UserId == userId))
        {
            throw new InvalidOperationException("Немає доступу до категорії фільтра.");
        }

        var presets = context.FilterPresets.Where(p => p.UserId == userId).ToList();
        var preset = presets.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (preset is null)
        {
            if (presets.Count >= 5)
            {
                throw new InvalidOperationException("Можна зберегти не більше 5 фільтрів.");
            }

            preset = new FilterPreset { UserId = userId, Name = name };
            context.FilterPresets.Add(preset);
        }
        preset.Search = query.Search;
        preset.CategoryId = query.CategoryId;
        preset.Priority = query.Priority;
        preset.Status = query.Status;
        preset.DateFilter = query.DateFilter;
        preset.SortBy = query.SortBy;
        preset.SortDescending = query.SortDescending;
        context.SaveChanges();
        transaction.Commit();
    }

    public void Delete(Guid id)
    {
        using var context = _createContext();
        context.FilterPresets.Where(p => p.Id == id && p.UserId == userId).ExecuteDelete();
    }
}
