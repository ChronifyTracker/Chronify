using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Chronify.ViewModels;
using Chronify.Views;

namespace Chronify;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            CreateDatabase();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(new SqliteTaskRepository())
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void CreateDatabase()
    {
        try
        {
            AppDbContext.CreateDatabaseIfMissing();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}
