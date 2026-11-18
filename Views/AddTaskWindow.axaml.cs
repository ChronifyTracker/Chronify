using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Chronify.ViewModels;

namespace Chronify.Views;

public partial class AddTaskWindow : Window
{
    public AddTaskWindow()
    {
        InitializeComponent();
        MinutesInput.Minimum = TaskItem.MinEstimatedMinutes;
        MinutesInput.Maximum = TaskItem.MaxEstimatedMinutes;
        Opened += (_, _) => TitleInput.Focus();
        NewCategoryInput.PropertyChanged += FocusNewCategoryInput;
    }

    private void FocusNewCategoryInput(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty && NewCategoryInput.IsVisible)
            NewCategoryInput.Focus();
    }

    private void Save(object? sender, RoutedEventArgs e)
    {
        var viewModel = (AddTaskViewModel)DataContext!;

        if (viewModel.Save())
            Close(true);
    }

    private void Cancel(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
