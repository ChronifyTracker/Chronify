using System.Security.Cryptography;
using System.Text;

namespace Chronify.Services;

public class ConnectionStore(Guid userId, Func<AppDbContext>? createContext = null)
{
    private readonly Func<AppDbContext> _createContext = createContext ?? (() => new AppDbContext());
    public ServiceConnection? Find(string provider)
    {
        using var db = _createContext();
        return db.ServiceConnections.Find(userId, provider);
    }

    public string Secret(string provider)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Хранение подключений пока поддерживается в Windows.");
        }

        var connection = Find(provider) ?? throw new InvalidOperationException("Сначала подключите сервис в личном кабинете.");
        return Encoding.UTF8.GetString(ProtectedData.Unprotect(
            Convert.FromBase64String(connection.ProtectedSecret),
            Entropy(),
            DataProtectionScope.CurrentUser
        ));
    }

    public void Save(
        string provider,
        string secret,
        string address = "",
        Guid? remoteUserId = null
    )
    {
        var prepared = Prepare(
            provider,
            secret,
            address,
            remoteUserId
        );
        using var db = _createContext();
        var row = db.ServiceConnections.Find(userId, provider);
        if (row is null)
        {
            row = new ServiceConnection { UserId = userId, Provider = provider };
            db.ServiceConnections.Add(row);
        }
        row.Address = address;
        row.RemoteUserId = remoteUserId;
        row.ProtectedSecret = prepared.ProtectedSecret;
        db.SaveChanges();
    }

    // Готуємо запис без SaveChanges, щоб акаунт і сесія Neon збереглися однією транзакцією.
    public ServiceConnection Prepare(
        string provider,
        string secret,
        string address,
        Guid? remoteUserId
    )
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Хранение подключений пока поддерживается в Windows.");
        }

        return new ServiceConnection
        {
            UserId = userId,
            Provider = provider,
            Address = address,
            RemoteUserId = remoteUserId,
            ProtectedSecret = Convert.ToBase64String(ProtectedData.Protect(
            Encoding.UTF8.GetBytes(secret),
            Entropy(),
            DataProtectionScope.CurrentUser
        ))
        };
    }

    public void Remove(string provider)
    {
        using var db = _createContext();
        var row = db.ServiceConnections.Find(userId, provider);
        if (row is not null)
        {
            db.Remove(row);
            db.SaveChanges();
        }
    }

    // Токен зашифровано DPAPI, додатково прив'язано до локального акаунта.
    private byte[] Entropy()
    {
        return userId.ToByteArray();
    }
}
