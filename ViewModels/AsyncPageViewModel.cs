using CommunityToolkit.Mvvm.ComponentModel;

namespace Chronify.ViewModels;

public partial class AsyncPageViewModel : PageViewModel
{
    [ObservableProperty]
    public partial bool IsBusy { get; set; }
    [ObservableProperty]
    public partial string? Message { get; set; }

    protected async Task RunAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        Message = null;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex switch
            {
                OperationCanceledException => "Операция отменена или истекло время ожидания. Локальные данные сохранены.",
                HttpRequestException => "Сервер недоступен. Локальные данные сохранены; повторите при появлении интернета.",
                Npgsql.NpgsqlException => "База Neon недоступна. Проверьте интернет и повторите подключение; локальные задачи сохранены.",
                System.Security.Cryptography.CryptographicException => "Windows не разрешила сохранить подключение. Запустите приложение в обычном профиле пользователя.",
                _ => ex.Message
            };
        }
        finally
        {
            IsBusy = false;
        }
    }
}
