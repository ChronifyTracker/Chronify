namespace Chronify
{
    public interface ICategoryRepository
    {
        void Add(Category category);
        Category? GetById(Guid categoryId);
        List<Category> GetAll();
        bool Update(Category category);
        bool Delete(Guid categoryId);
    }
}
