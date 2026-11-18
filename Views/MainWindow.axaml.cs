using Avalonia.Controls;
using Avalonia.Interactivity;
using Chronify.ViewModels;

namespace Chronify.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    private void AddTask(object? sender, RoutedEventArgs e)
    {
        ShowTaskDialog(ViewModel.CreateAddTask());
    }

    private void EditTask(object? sender, RoutedEventArgs e)
    {
        var button = (Button)sender!;
        var card = (TaskCard)button.DataContext!;

        ShowTaskDialog(ViewModel.CreateEditTask(card.Id));
    }

    private async void ShowTaskDialog(AddTaskViewModel? dialog)
    {
        if (dialog is null)
        {
            ViewModel.Refresh();
            return;
        }

        var window = new AddTaskWindow { DataContext = dialog };
        var saved = await window.ShowDialog<bool>(this);

        if (saved)
            ViewModel.Refresh();
    }
}
