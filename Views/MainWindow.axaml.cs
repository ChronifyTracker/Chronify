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

    private async void AddTask(object? sender, RoutedEventArgs e)
    {
        var viewModel = (MainWindowViewModel)DataContext!;
        var window = new AddTaskWindow { DataContext = viewModel.CreateAddTask() };

        if (await window.ShowDialog<bool>(this))
            viewModel.Refresh();
    }
}
