using Chronify.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Chronify.ViewModels;

public partial class AccountViewModel : PageViewModel
{
    private readonly UserAccount _user;
    private readonly AuthenticationService _authentication;
    private readonly UserPreferencesRepository _preferences;
    private bool _languageReady;
    public IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new("ru", "Русский"),
        new("uk", "Українська"),
        new("en", "English")
    ];
    [ObservableProperty]
    public partial LanguageOption? SelectedLanguage { get; set; }
    public IntegrationsViewModel Integrations { get; }
    [ObservableProperty]
    public partial string DisplayName { get; set; }
    [ObservableProperty]
    public partial string? Message { get; set; }
    public string Email => _user.Email;

    public string CreatedAt => Texts.Date(_user.CreatedAt);

    public event Action? LogoutRequested;

    public AccountViewModel(
        UserAccount user,
        AuthenticationService authentication,
        Func<AppDbContext>? createContext = null
    )
    {
        _user = user;
        _authentication = authentication;
        _preferences = new UserPreferencesRepository(user.Id, createContext);
        string languageCode = _preferences.Get().LanguageCode;
        SelectedLanguage = Languages.FirstOrDefault(language => language.Code == languageCode) ?? Languages[0];
        Integrations = new IntegrationsViewModel(user.Id, createContext);
        DisplayName = user.DisplayName;
        _languageReady = true;
    }

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (!_languageReady || value is null)
        {
            return;
        }

        // Спочатку зберігаємо вибір. За помилки БД поточна мова не змінюється.
        string previousCode = LocalizationService.Current.LanguageCode;
        if (Run(() => _preferences.SaveLanguage(value.Code)))
        {
            LocalizationService.Current.ChangeLanguage(value.Code);
        }
        else
        {
            _languageReady = false;
            SelectedLanguage = Languages.First(language => language.Code == previousCode);
            _languageReady = true;
        }
    }

    public override void RefreshLanguage()
    {
        Message = null;
        base.RefreshLanguage();
    }

    [RelayCommand]
    private void SaveProfile()
    {
        if (Run(() => _authentication.UpdateName(_user, DisplayName)))
        {
            Message = Texts.Get("Account.NameSaved");
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

public record LanguageOption(string Code, string Name);
