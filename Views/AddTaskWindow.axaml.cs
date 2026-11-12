using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Chronify.Views;

public partial class AddTaskWindow : Window
{
    private readonly ITaskRepository? _repository;

    public AddTaskWindow()
    {
        InitializeComponent();
        Opened += (_, _) => TitleInput.Focus();
    }

    public AddTaskWindow(ITaskRepository repository) : this()
    {
        _repository = repository;
    }

    private void Save(object? sender, RoutedEventArgs e)
    {
        if (_repository is null)
        {
            ShowError("База задач недоступна.");
            return;
        }

        if (string.IsNullOrWhiteSpace(TitleInput.Text))
        {
            ShowError("Введите название.");
            TitleInput.Focus();
            return;
        }

        if (!MinutesInput.Value.HasValue || MinutesInput.Value != decimal.Truncate(MinutesInput.Value.Value))
        {
            ShowError("Укажите целое число минут.");
            return;
        }

        DateTime? dueDate = null;
        if (!string.IsNullOrWhiteSpace(DateInput.Text) && DateInput.SelectedDate is null)
        {
            ShowError("Проверьте дату.");
            return;
        }
        if (DateInput.SelectedDate is DateTime date)
            dueDate = date.Date + (TimeInput.SelectedTime ?? TimeSpan.FromHours(18));

        try
        {
            var task = new TaskItem(
                TitleInput.Text,
                DescriptionInput.Text ?? "",
                dueDate: dueDate,
                estimatedMinutes: (int)MinutesInput.Value.Value,
                priority: (TaskPriority)PriorityInput.SelectedIndex);

            _repository.Add(task);
            Close(true);
        }
        catch (ArgumentException)
        {
            ShowError("Проверьте данные задачи.");
        }
        catch (Exception)
        {
            ShowError("Не удалось сохранить задачу.");
        }
    }

    private void Cancel(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
