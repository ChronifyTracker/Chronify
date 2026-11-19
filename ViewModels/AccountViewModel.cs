using Chronify.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Chronify.ViewModels;

public partial class AccountViewModel : PageViewModel
{
    private readonly UserAccount _user;
    private readonly AuthenticationService _authentication;
    public IntegrationsViewModel Integrations { get; }
    [ObservableProperty]
    public partial string DisplayName { get; set; }
    [ObservableProperty]
    public partial string? Message { get; set; }
    public string Email => _user.Email;

    public string CreatedAt => _user.CreatedAt.ToString("dd.MM.yyyy");

    public event Action? LogoutRequested;

    public AccountViewModel(
        UserAccount user,
        AuthenticationService authentication,
        Func<AppDbContext>? createContext = null
    )
    {
        _user = user;
        _authentication = authentication;
        Integrations = new IntegrationsViewModel(user.Id, createContext);
        DisplayName = user.DisplayName;
    }

    [RelayCommand]
    private void SaveProfile()
    {
        if (Run(() => _authentication.UpdateName(_user, DisplayName)))
        {
            Message = "Имя сохранено.";
        }
    }

    [RelayCommand]
    private async Task Logout()
    {
        try
        {
            await _authentication.LogoutAsync();
            LogoutRequested?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
