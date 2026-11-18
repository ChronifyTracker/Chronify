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
    public string Heading => IsRegistration ? "Создать аккаунт" : "Вход в Chronify";

    public string SubmitText => IsRegistration ? "Зарегистрироваться" : "Войти";

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
            ErrorMessage = "Пароли не совпадают.";
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
                ? "Не удалось открыть аккаунт. Проверьте доступ к базе данных."
                : ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
