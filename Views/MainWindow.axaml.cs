using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Chronify.Views;

public partial class MainWindow : Window
{
    private ITaskRepository? _repository;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMinutes(1) };

    public MainWindow()
    {
        InitializeComponent();
        try
        {
            _repository = new SqliteTaskRepository();
            RefreshTasks();
        }
        catch (Exception)
        {
            ShowError("Не удалось открыть базу задач.");
        }

        _timer.Tick += (_, _) => RefreshTasks();
        _timer.Start();
        Closed += (_, _) => _timer.Stop();
    }

    private void RefreshTasks()
    {
        DateText.Text = DateTime.Now.ToString("dddd, d MMMM", CultureInfo.GetCultureInfo("ru-RU"));
        if (_repository is null)
            return;

        try
        {
            var tasks = _repository.GetAll(new TaskQuery
            {
                SortBy = TaskSortBy.CreatedAt,
                SortDescending = true
            });
            var urgent = tasks.Where(t => t.Status != TaskStatus.Completed &&
                (t.Priority >= TaskPriority.High || t.DueDate <= DateTime.Now.AddDays(1)))
                .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                .ThenByDescending(t => t.Priority).ToList();
            var newTasks = tasks.Where(t => t.Status == TaskStatus.New).ToList();

            UrgentTasks.ItemsSource = urgent.Select(t => new TaskCard(t)).ToList();
            NewTasks.ItemsSource = newTasks.Select(t => new TaskCard(t)).ToList();
            AllTasks.ItemsSource = tasks.Select(t => new TaskCard(t)).ToList();
            UrgentCount.Text = urgent.Count.ToString("D2");
            NewCount.Text = newTasks.Count.ToString("D2");
            TotalCount.Text = tasks.Count.ToString("D2");
            UrgentEmpty.IsVisible = urgent.Count == 0;
            NewEmpty.IsVisible = newTasks.Count == 0;
            AllEmpty.IsVisible = tasks.Count == 0;
            ErrorText.IsVisible = false;
        }
        catch (Exception)
        {
            ShowError("Не удалось загрузить задачи.");
        }
    }

    private void ShowHome(object? sender, RoutedEventArgs e)
    {
        HomePanel.IsVisible = true;
        AllPanel.IsVisible = false;
        PageTitle.Text = "Главная";
        HomeButton.Classes.Add("selected");
        AllButton.Classes.Remove("selected");
        RefreshTasks();
    }

    private void ShowAll(object? sender, RoutedEventArgs e)
    {
        HomePanel.IsVisible = false;
        AllPanel.IsVisible = true;
        PageTitle.Text = "Все задачи";
        HomeButton.Classes.Remove("selected");
        AllButton.Classes.Add("selected");
        RefreshTasks();
    }

    private async void AddTask(object? sender, RoutedEventArgs e)
    {
        if (_repository is null)
        {
            ShowError("База задач недоступна. Перезапустите приложение.");
            return;
        }

        var window = new AddTaskWindow(_repository);
        if (await window.ShowDialog<bool>(this))
            RefreshTasks();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
