using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Chronify.Services;
using Chronify.ViewModels;
using Chronify.Views;

namespace Chronify;

public partial class App : Application
{
    private AuthenticationService? _authentication;
    private IClassicDesktopStyleApplicationLifetime? _desktop;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Прев'ю має лише показувати XAML, без входу в акаунт та оновлення робочої БД.
        if (!Design.IsDesignMode && ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktop = desktop;
            _authentication = new AuthenticationService(new SessionTokenStore(Path.GetFullPath("chronify.session.bin")));
            try
            {
                AppDbContext.InitializeDatabase();
                var user = _authentication.RestoreSession();
                if (user is null)
                {
                    ShowLogin();
                }
                else
                {
                    ShowWorkspace(user);
                }
            }
            catch (Exception)
            {
                ShowLogin(Texts.Get("Errors.Startup"));
            }
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void ShowLogin(string? error = null)
    {
        var viewModel = new LoginViewModel(_authentication!) { ErrorMessage = error };
        viewModel.SignedIn += ShowWorkspace;
        ReplaceWindow(new LoginWindow { DataContext = viewModel });
    }

    private void ShowWorkspace(UserAccount user)
    {
        var viewModel = new MainWindowViewModel(
            user,
            new SqliteTaskRepository(user.Id),
            new SqliteCategoryRepository(user.Id),
            new WorkSessionRepository(user.Id),
            new FilterPresetRepository(user.Id),
            new UserPreferencesRepository(user.Id),
            _authentication!
        );
        viewModel.LogoutRequested += () => ShowLogin();
        ReplaceWindow(new MainWindow { DataContext = viewModel });
    }

    private void ReplaceWindow(Window window)
    {
        var previous = _desktop!.MainWindow;
        _desktop.MainWindow = window;
        if (previous is not null)
        {
            window.Show();
            previous.Close();
        }
    }
}
