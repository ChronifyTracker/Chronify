using Microsoft.EntityFrameworkCore;

namespace Chronify
{
    public class SqliteCategoryRepository : ICategoryRepository
    {
        public void Add(Category category)
        {
            using var context = new AppDbContext();

            if (IsNameTaken(context, category))
            {
                throw new ModelValidationException(ModelError.CategoryNameTaken);
            }

            context.Categories.Add(category);
            context.SaveChanges();
        }

        public Category? GetById(Guid categoryId)
        {
            using var context = new AppDbContext();

            return context.Categories.Find(categoryId);
        }

        public List<Category> GetAll()
        {
            using var context = new AppDbContext();

            return context.Categories
                .ToList()
                .OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public bool Update(Category category)
        {
            using var context = new AppDbContext();

            if (!context.Categories.Any(c => c.Id == category.Id))
            {
                return false;
            }

            if (IsNameTaken(context, category))
            {
                throw new ModelValidationException(ModelError.CategoryNameTaken);
            }

            context.Categories.Update(category);
            context.SaveChanges();
            return true;
        }

        public bool Delete(Guid categoryId)
        {
            using var context = new AppDbContext();

            var category = context.Categories.Find(categoryId);

            if (category is null)
            {
                return false;
            }

            context.Tasks
                .Where(t => t.CategoryId == categoryId)
                .Load();

            context.Categories.Remove(category);
            context.SaveChanges();
            return true;
        }

        private static bool IsNameTaken(AppDbContext context, Category category)
        {
            var otherNames = context.Categories
                .Where(c => c.Id != category.Id)
                .Select(c => c.Name)
                .ToList();

            return otherNames.Any(name => string.Equals(name, category.Name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
