using Avalonia.Controls;
using Avalonia.Interactivity;
using Chronify.ViewModels;

namespace Chronify.Views;

public partial class TaskDetailsWindow : Window
{
    public TaskDetailsWindow()
    {
        InitializeComponent();
    }

    private async void OpenAttachment(object? sender, RoutedEventArgs e)
    {
        if (DataContext is TaskDetailsViewModel model && sender is Control { DataContext: AttachmentInfo file })
        {
            await model.OpenAttachmentCommand.ExecuteAsync(file.Id);
        }
    }
}
