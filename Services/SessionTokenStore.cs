using System.Security.Cryptography;
using System.Text;

namespace Chronify.Services;

public interface ISessionTokenStore
{
    string? Read();
    void Write(string token);
    void Clear();
}

// DPAPI захищає токен ключем поточного облікового запису Windows.
public class SessionTokenStore(string path) : ISessionTokenStore
{
    public string? Read()
    {
        if (!File.Exists(path) || !OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(
                File.ReadAllBytes(path),
                null,
                DataProtectionScope.CurrentUser
            ));
        }
        catch (CryptographicException)
        {
            Clear();
            return null;
        }
    }

    public void Write(string token)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(Texts.Get("Errors.RememberWindows"));
        }

        byte[] data = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(path, data);
    }

    public void Clear()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
