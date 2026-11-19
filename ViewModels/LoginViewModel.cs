using Chronify.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Chronify.ViewModels;

public partial class LoginViewModel(AuthenticationService authentication) : PageViewModel
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Heading)), NotifyPropertyChangedFor(nameof(SubmitText))]
    public partial bool IsRegistration { get; set; }
    [ObservableProperty]
    public partial string Name { get; set; } = "";
    [ObservableProperty]
    public partial string Email { get; set; } = "";
    [ObservableProperty]
    public partial string Password { get; set; } = "";
    [ObservableProperty]
    public partial string PasswordConfirmation { get; set; } = "";
    [ObservableProperty]
    public partial bool RememberMe { get; set; } = true;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    public partial bool IsBusy { get; set; }
    public string Heading => IsRegistration ? Texts.Get("Login.CreateAccount") : Texts.Get("Login.Heading");

    public string SubmitText => IsRegistration ? Texts.Get("Login.Register") : Texts.Get("Login.SignIn");

    public event Action<UserAccount>? SignedIn;

    private bool CanSubmit()
    {
        return !IsBusy;
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        ErrorMessage = null;
        if (IsRegistration && Password != PasswordConfirmation)
        {
            ErrorMessage = Texts.Get("Errors.PasswordConfirmation");
            return;
        }
        IsBusy = true;
        try
        {
            // Обчислення хешу не блокує потік інтерфейсу.
            bool registration = IsRegistration;
            string name = Name, email = Email, password = Password;
            bool remember = RememberMe;
            var user = await Task.Run(() => registration
                ? authentication.RegisterAsync(
                name,
                email,
                password,
                remember
            )
                : authentication.LoginAsync(email, password, remember));
            Password = "";
            PasswordConfirmation = "";
            SignedIn?.Invoke(user);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex is SqliteException or DbUpdateException
                ? Texts.Get("Errors.AccountOpen")
                : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
