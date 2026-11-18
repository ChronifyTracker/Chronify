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
        ShowTaskDialog(ViewModel.CreateEditTask(CardOf(sender).Id));
    }

    private void StartTask(object? sender, RoutedEventArgs e)
    {
        ViewModel.StartTask(CardOf(sender).Id);
    }

    private void CompleteTask(object? sender, RoutedEventArgs e)
    {
        ViewModel.CompleteTask(CardOf(sender).Id);
    }

    private void ReopenTask(object? sender, RoutedEventArgs e)
    {
        ViewModel.ReopenTask(CardOf(sender).Id);
    }

    private void DeleteTask(object? sender, RoutedEventArgs e)
    {
        ViewModel.DeleteTask(CardOf(sender).Id);
    }

    private static TaskCard CardOf(object? sender)
    {
        var control = (Control)sender!;

        return (TaskCard)control.DataContext!;
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
