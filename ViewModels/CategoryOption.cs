namespace Chronify.ViewModels;

public class CategoryOption
{
    public string Name { get; }
    public Category? Category { get; }
    public bool IsAddNew { get; }

    public CategoryOption(string name, Category? category = null, bool isAddNew = false)
    {
        Name = name;
        Category = category;
        IsAddNew = isAddNew;
    }
}
