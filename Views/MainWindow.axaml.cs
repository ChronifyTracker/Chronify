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

    private void ShowHome(object? sender, RoutedEventArgs e)
    {
        HomePanel.IsVisible = true;
        AllPanel.IsVisible = false;
        PageTitle.Text = "Главная";
        HomeButton.Classes.Add("selected");
        AllButton.Classes.Remove("selected");
        _viewModel.Refresh();
    }

    private void ShowAll(object? sender, RoutedEventArgs e)
    {
        HomePanel.IsVisible = false;
        AllPanel.IsVisible = true;
        PageTitle.Text = "Все задачи";
        HomeButton.Classes.Remove("selected");
        AllButton.Classes.Add("selected");
        _viewModel.Refresh();
    }

    private async void AddTask(object? sender, RoutedEventArgs e)
    {
        var window = new AddTaskWindow(_repository);
        if (await window.ShowDialog<bool>(this))
            _viewModel.Refresh();
    }
}
