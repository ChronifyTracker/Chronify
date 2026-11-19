using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Chronify.ViewModels;

public partial class CategoriesViewModel(ICategoryRepository repository) : PageViewModel
{
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<string> Colors { get; } =

    [

        "#F34F78",

        "#64B5F6",

        "#81C784",

        "#FFB74D",

        "#BA68C8",

        "#4DD0E1",

        "#A1887F",

        "#90A4AE"

    ];
    [ObservableProperty]
    public partial Category? SelectedCategory { get; set; }
    [ObservableProperty]
    public partial string Name { get; set; } = "";
    [ObservableProperty]
    public partial string Color { get; set; } = "#F34F78";
    public event Action? DataChanged;

    partial void OnSelectedCategoryChanged(Category? value)
    {
        Name = value?.Name ?? "";
        if (value is not null && !Colors.Contains(value.Color))
        {
            Colors.Add(value.Color);
        }

        Color = value?.Color ?? Colors[0];
    }

    public void Refresh()
    {
        Run(() =>
        {
            Guid? selectedId = SelectedCategory?.Id;
            Categories.Clear();
            foreach (var category in repository.GetAll())
            {
                Categories.Add(category);
            }
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == selectedId);
        });
    }

    [RelayCommand]
    private void NewCategory()
    {
        SelectedCategory = null;
        Name = "";
        Color = Colors[0];
    }

    [RelayCommand]
    private void Save()
    {
        if (Run(() =>
        {
            var category = new Category(Name, Color);
            if (SelectedCategory is null)
            {
                repository.Add(category);
            }
            else if (!repository.Update(new Category(Name, Color) { Id = SelectedCategory.Id }))
            {
                throw new InvalidOperationException(Texts.Get("Errors.CategoryDeleted"));
            }
        }))
        {
            Refresh();
            NewCategory();
            DataChanged?.Invoke();
        }
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedCategory is null)
        {
            ErrorMessage = Texts.Get("Errors.SelectCategory");
            return;
        }
        if (Run(() => repository.Delete(SelectedCategory.Id)))
        {
            Refresh();
            NewCategory();
            DataChanged?.Invoke();
        }
    }
}
