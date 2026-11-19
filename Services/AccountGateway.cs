using Chronify.Teams;
using Npgsql;

namespace Chronify.Services;

public interface IAccountGateway
{
    string Workspace { get; }
    Task<CloudSession> Register(CloudCredentials credentials);
    Task<CloudSession> Login(CloudCredentials credentials, bool registerLegacyAccount);
    Task Logout(string token);
}

public sealed class AccountUnavailableException(string message, Exception? inner = null) : IOException(
    message,
    inner
)
{
}

public class NeonAccountGateway : IAccountGateway
{
    public string Workspace => NeonConfiguration.Workspace;

    public Task<CloudSession> Register(CloudCredentials credentials)
    {
        return Authenticate("register", credentials);
    }

    public Task<CloudSession> Login(CloudCredentials credentials, bool registerLegacyAccount)
    {
        return Authenticate(registerLegacyAccount ? "loginOrRegister" : "login", credentials);
    }

    public async Task Logout(string token)
    {
        await NeonGateway.Request<object>("logout", token: token).ConfigureAwait(false);
    }

    private static async Task<CloudSession> Authenticate(string action, CloudCredentials credentials)
    {
        if (string.IsNullOrWhiteSpace(NeonConfiguration.Read("CHRONIFY_DATABASE_URL")))
        {
            throw new AccountUnavailableException(Texts.Get("Errors.DatabaseNotConfigured"));
        }

        try
        {
            return await NeonGateway.Request<CloudSession>(action, credentials).ConfigureAwait(false);
        }
        // Неправильний пароль БД або відмова реєстрації не є відсутністю інтернету.
        catch (NpgsqlException ex) when (ex is not PostgresException)
        {
            throw new AccountUnavailableException(Texts.Get("Errors.DatabaseUnavailable"), ex);
        }
        catch (TimeoutException ex)
        {
            throw new AccountUnavailableException(Texts.Get("Errors.DatabaseTimeout"), ex);
        }
    }
}
