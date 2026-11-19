using Microsoft.EntityFrameworkCore;

namespace Chronify;

public class SqliteCategoryRepository : ICategoryRepository
{
    private readonly Guid? _userId;
    private readonly Func<AppDbContext> _createContext;

    public SqliteCategoryRepository(Guid? userId = null, Func<AppDbContext>? createContext = null)
    {
        _userId = userId;
        _createContext = createContext ?? (() => new AppDbContext());
    }

    public void Add(Category category)
    {
        using var context = _createContext();
        if (category.UserId is not null && category.UserId != _userId)
        {
            throw new InvalidOperationException(Texts.Get("Errors.CategoryAccess"));
        }

        category.UserId = _userId;
        CheckName(context, category);
        context.Categories.Add(category);
        context.SaveChanges();
    }

    public Category? GetById(Guid categoryId)
    {
        using var context = _createContext();
        return context.Categories.AsNoTracking().FirstOrDefault(c => c.Id == categoryId && c.UserId == _userId);
    }

    public List<Category> GetAll()
    {
        using var context = _createContext();
        return context.Categories.AsNoTracking().Where(c => c.UserId == _userId)
            .ToList().OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public bool Update(Category category)
    {
        using var context = _createContext();
        var stored = context.Categories.FirstOrDefault(c => c.Id == category.Id && c.UserId == _userId);
        if (stored is null)
        {
            return false;
        }

        if (stored.Name != category.Name
            && context.Tasks.Any(t => t.UserId == _userId
            && t.CategoryId == category.Id
            && t.GroupAssignmentId != null))
        {
            throw new InvalidOperationException(Texts.Get("Errors.GroupCategoryName"));
        }

        category.UserId = _userId;
        CheckName(context, category);
        context.Entry(stored).CurrentValues.SetValues(category);
        context.SaveChanges();
        return true;
    }

    public bool Delete(Guid categoryId)
    {
        using var context = _createContext();
        var category = context.Categories.FirstOrDefault(c => c.Id == categoryId && c.UserId == _userId);
        if (category is null)
        {
            return false;
        }

        if (context.Tasks.Any(t => t.UserId == _userId && t.CategoryId == categoryId && t.GroupAssignmentId != null))
        {
            throw new InvalidOperationException(Texts.Get("Errors.GroupCategoryDelete"));
        }

        context.Categories.Remove(category);
        context.SaveChanges();
        return true;
    }

    private void CheckName(AppDbContext context, Category category)
    {
        var names = context.Categories.Where(c => c.UserId == _userId && c.Id != category.Id)
            .Select(c => c.Name).ToList();
        if (names.Any(name => string.Equals(name, category.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ModelValidationException(ModelError.CategoryNameTaken);
        }
    }
}
