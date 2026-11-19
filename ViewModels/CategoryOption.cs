namespace Chronify.ViewModels;

public class CategoryOption : LocalizedObservableObject
{
    private readonly string _name;
    private readonly string? _translationKey;
    public string Name => _translationKey is null ? _name : Texts.Get(_translationKey);
    public Category? Category { get; }
    public bool IsAddNew { get; }

    public CategoryOption(
        string name,
        Category? category = null,
        bool isAddNew = false,
        string? translationKey = null
    )
    {
        _name = name;
        _translationKey = translationKey;
        Category = category;
        IsAddNew = isAddNew;
    }
}
