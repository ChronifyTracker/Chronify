using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Chronify.ViewModels;

namespace Chronify.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMinutes(1) };
    private readonly DispatcherTimer _groupsTimer = new() { Interval = TimeSpan.FromSeconds(5) };

    public MainWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
        {
            return;
        }

        _timer.Tick += (_, _) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.Refresh();
            }
        };
        _groupsTimer.Tick += RefreshGroups;
        Opened += (_, _) =>
        {
            _timer.Start();
            _groupsTimer.Start();
        };
        Closed += (_, _) =>
        {
            _timer.Stop();
            _groupsTimer.Stop();
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.GroupsPage.StopAutomaticRefresh();
            }
        };
    }

    private async void RefreshGroups(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel { IsGroupsPage: true } viewModel)
        {
            await viewModel.GroupsPage.RefreshAutomatically();
        }
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

    private async void ReadTask(object? sender, RoutedEventArgs e)
    {
        var model = ViewModel.TaskDetails(CardOf(sender).Id);
        if (model is not null)
        {
            await new TaskDetailsWindow { DataContext = model }.ShowDialog(this);
        }
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
        {
            ViewModel.Refresh();
        }
    }
}
