using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Chronify.ViewModels;

namespace Chronify.Views;

public partial class MainWindow : Window
{
    private readonly ITaskRepository _repository = new SqliteTaskRepository();
    private readonly MainWindowViewModel _viewModel;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMinutes(1) };

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainWindowViewModel(_repository);
        DataContext = _viewModel;

        _timer.Tick += (_, _) => _viewModel.Refresh();
        _timer.Start();
        Closed += (_, _) => _timer.Stop();
    }

    private async void AddTask(object? sender, RoutedEventArgs e)
    {
        var window = new AddTaskWindow(_repository);
        if (await window.ShowDialog<bool>(this))
            _viewModel.Refresh();
    }
}
