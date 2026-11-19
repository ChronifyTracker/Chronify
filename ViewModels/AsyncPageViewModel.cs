using CommunityToolkit.Mvvm.ComponentModel;

namespace Chronify.ViewModels;

public partial class AsyncPageViewModel : PageViewModel
{
    [ObservableProperty]
    public partial bool IsBusy { get; set; }
    [ObservableProperty]
    public partial string? Message { get; set; }

    public override void RefreshLanguage()
    {
        Message = null;
        base.RefreshLanguage();
    }

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
                OperationCanceledException => Texts.Get("Errors.Cancelled"),
                HttpRequestException => Texts.Get("Errors.ServerUnavailable"),
                Npgsql.NpgsqlException => Texts.Get("Errors.NeonUnavailable"),
                System.Security.Cryptography.CryptographicException => Texts.Get("Errors.ProtectedConnection"),
                _ => ex.Message
            };
        }
        finally
        {
            IsBusy = false;
        }
    }
}
