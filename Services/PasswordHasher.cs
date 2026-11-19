using System.Security.Cryptography;

namespace Chronify.Services;

public static class PasswordHasher
{
    public const int Iterations = 600_000;

    public static (string Hash, string Salt) Create(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length is < 8 or > 128)
        {
            throw new ArgumentException(Texts.Get("Errors.PasswordLength"));
        }

        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            32
        );
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool Verify(string password, UserAccount user)
    {
        if (password.Length > 128)
        {
            return false;
        }

        byte[] expected = Convert.FromBase64String(user.PasswordHash);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            Convert.FromBase64String(user.PasswordSalt),
            user.PasswordIterations,
            HashAlgorithmName.SHA256,
            expected.Length
        );
        return CryptographicOperations.FixedTimeEquals(expected, hash);
    }
}
