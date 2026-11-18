using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Chronify.Teams;
using Microsoft.EntityFrameworkCore;

namespace Chronify.Services;

public class AuthenticationService
{
    private readonly Func<AppDbContext> _createContext;
    private readonly ISessionTokenStore _tokenStore;
    private readonly IAccountGateway _accounts;
    private string? _currentToken;
    private Guid? _currentUserId;
    public string? SessionNotice { get; private set; }

    public AuthenticationService(
        ISessionTokenStore tokenStore,
        Func<AppDbContext>? createContext = null,
        IAccountGateway? accounts = null
    )
    {
        _tokenStore = tokenStore;
        _createContext = createContext ?? (() => new AppDbContext());
        _accounts = accounts ?? new NeonAccountGateway();
    }

    public UserAccount Register(
        string name,
        string email,
        string password,
        bool remember
    )
    {
        return RegisterAsync(
            name,
            email,
            password,
            remember
        ).GetAwaiter().GetResult();
    }

    public async Task<UserAccount> RegisterAsync(
        string name,
        string email,
        string password,
        bool remember
    )
    {
        name = name.Trim();
        email = email.Trim();
        if (name.Length is < 1 or > 80)
        {
            throw new ArgumentException("Ім'я: від 1 до 80 символів.");
        }

        if (!MailAddress.TryCreate(email, out var address) || address.Address != email || email.Length > 200)
        {
            throw new ArgumentException("Введіть коректну електронну адресу.");
        }

        var credentials = PasswordHasher.Create(password);
        if (Encoding.UTF8.GetByteCount(password) > 72)
        {
            throw new ArgumentException("Пароль не должен превышать 72 байта UTF-8.");
        }

        using (var context = _createContext())
        {
            if (context.Users.Any(u => u.NormalizedEmail == email.ToUpperInvariant()))
            {
                throw new InvalidOperationException("Аккаунт уже существует. Используйте обычный вход.");
            }
        }
        // Спочатку загальна БД. Без мережі не створюємо акаунт лише на цьому ПК.
        CloudSession session;
        try
        {
            session = await _accounts.Register(new CloudCredentials(email, password, name)).ConfigureAwait(false);
        }
        catch (AccountUnavailableException ex)
        {
            throw new InvalidOperationException(
              "Для регистрации нужен интернет: аккаунт создаётся в общей и локальной БД.",
              ex
          );
        }
        var user = SaveAuthenticatedUser(
            null,
            email,
            name,
            credentials,
            session
        );
        StartSession(user, remember);
        return user;
    }

    public UserAccount Login(
        string email,
        string password,
        bool remember
    )
    {
        return LoginAsync(email, password, remember).GetAwaiter().GetResult();
    }

    public async Task<UserAccount> LoginAsync(
        string email,
        string password,
        bool remember
    )
    {
        email = email.Trim();
        UserAccount? local;
        ServiceConnection? binding;
        using (var context = _createContext())
        {
            local = context.Users.AsNoTracking().FirstOrDefault(u => u.NormalizedEmail == email.ToUpperInvariant());
            binding = local is null ? null : context.ServiceConnections.Find(local.Id, "ChronifyNeon");
        }
        bool localPasswordValid = local is not null && PasswordHasher.Verify(password, local);
        CloudSession session;
        try
        {
            // Старі локальні дані прив'язуємо лише після підтвердження їхнього пароля.
            session = await _accounts.Login(new CloudCredentials(email, password, local?.DisplayName ?? ""),
                registerLegacyAccount: localPasswordValid && binding is null).ConfigureAwait(false);
        }
        catch (AccountUnavailableException)
        {
            if (!localPasswordValid)
            {
                throw new InvalidOperationException("Нет связи с общей базой. Для первого входа нужен интернет; для сохранённого аккаунта проверьте пароль.");
            }

            StartSession(local!, remember);
            SessionNotice = (SessionNotice is null ? "" : SessionNotice + " ") + "Вход без интернета. Сохранённые задачи доступны; группы обновятся при подключении.";
            return local!;
        }
        if (local is not null && binding is null && !localPasswordValid)
        {
            throw new InvalidOperationException("Пароль общей учётной записи не совпадает с паролем старых локальных данных. Автоматическая привязка отменена.");
        }

        var credentials = PasswordHasher.Create(password);
        var user = SaveAuthenticatedUser(
            local,
            email,
            session.Name,
            credentials,
            session
        );
        StartSession(user, remember);
        return user;
    }

    private UserAccount SaveAuthenticatedUser(
        UserAccount? local,
        string email,
        string name,
        (string Hash, string Salt) credentials,
        CloudSession session
    )
    {
        Guid localId = local?.Id ?? session.UserId;
        var protectedSession = new ConnectionStore(localId, _createContext).Prepare(
            "ChronifyNeon",
            session.Token,
            _accounts.Workspace,
            session.UserId
        );
        using var context = _createContext();
        using var transaction = context.Database.BeginTransaction();
        var binding = context.ServiceConnections.Find(localId, "ChronifyNeon");
        if (binding is not null
            && (binding.RemoteUserId != session.UserId
            || binding.Address != _accounts.Workspace))
        {
            throw new InvalidOperationException("Локальные данные уже связаны с другой учётной записью или базой.");
        }

        bool firstAccount = !context.Users.Any();
        var user = local is null ? new UserAccount { Id = localId } : context.Users.Single(u => u.Id == localId);
        user.DisplayName = local?.DisplayName ?? (string.IsNullOrWhiteSpace(name) ? email : name);
        user.Email = email;
        user.NormalizedEmail = email.ToUpperInvariant();
        user.PasswordHash = credentials.Hash;
        user.PasswordSalt = credentials.Salt;
        user.PasswordIterations = PasswordHasher.Iterations;
        if (local is null)
        {
            context.Users.Add(user);
        }

        if (binding is null)
        {
            context.ServiceConnections.Add(protectedSession);
        }
        else
        {
            context.Entry(binding).CurrentValues.SetValues(protectedSession);
        }

        context.SaveChanges();
        // Зберігаємо старі дані: перший користувач отримує задачі попередньої версії.
        if (firstAccount)
        {
            context.Tasks.Where(t => t.UserId == null).ExecuteUpdate(s => s.SetProperty(t => t.UserId, user.Id));
            context.Categories.Where(c => c.UserId == null).ExecuteUpdate(s => s.SetProperty(
                c => c.UserId,
                user.Id
            ));
        }
        transaction.Commit();
        return user;
    }

    public UserAccount? RestoreSession()
    {
        string? token = _tokenStore.Read();
        if (token is null)
        {
            return null;
        }

        using var context = _createContext();
        string hash = HashToken(token);
        var session = context.UserSessions.AsNoTracking()
            .FirstOrDefault(s => s.TokenHash == hash && s.ExpiresAtUtc > DateTime.UtcNow);
        if (session is null)
        {
            _tokenStore.Clear();
            return null;
        }
        _currentToken = token;
        _currentUserId = session.UserId;
        return context.Users.AsNoTracking().FirstOrDefault(u => u.Id == session.UserId);
    }

    public void Logout()
    {
        LogoutAsync().GetAwaiter().GetResult();
    }

    public async Task LogoutAsync()
    {
        string? remoteToken = null;
        if (_currentUserId is Guid userId)
        {
            var connections = new ConnectionStore(userId, _createContext);
            if (connections.Find("ChronifyNeon") is not null)
            {
                try
                {
                    remoteToken = connections.Secret("ChronifyNeon");
                }
                catch (Exception ex) when (ex is CryptographicException or PlatformNotSupportedException) { }
                // Зв'язок ідентичностей зберігаємо, відкликаний токен прибираємо.
                using var context = _createContext();
                var binding = context.ServiceConnections.Find(userId, "ChronifyNeon")!;
                binding.ProtectedSecret = "";
                context.SaveChanges();
            }
        }
        if (_currentToken is not null)
        {
            using var context = _createContext();
            string hash = HashToken(_currentToken);
            context.UserSessions.Where(s => s.TokenHash == hash).ExecuteDelete();
        }
        _currentToken = null;
        _currentUserId = null;
        _tokenStore.Clear();
        if (remoteToken is not null)
        {
            try
            {
                await _accounts.Logout(remoteToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is AccountUnavailableException or Npgsql.NpgsqlException or TimeoutException or InvalidOperationException)
            { /* Локальний вихід працює й без мережі. Віддалений токен має обмежений строк дії. */ }
        }
    }

    public void UpdateName(UserAccount user, string name)
    {
        name = name.Trim();
        if (name.Length is < 1 or > 80)
        {
            throw new ArgumentException("Ім'я: від 1 до 80 символів.");
        }

        using var context = _createContext();
        var stored = context.Users.Single(u => u.Id == user.Id);
        stored.DisplayName = name;
        context.SaveChanges();
        user.DisplayName = name;
    }

    private void StartSession(UserAccount user, bool remember)
    {
        string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        using var context = _createContext();
        context.UserSessions.Where(s => s.ExpiresAtUtc <= DateTime.UtcNow).ExecuteDelete();
        context.UserSessions.Add(new UserSession
        {
            UserId = user.Id,
            TokenHash = HashToken(token),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        });
        context.SaveChanges();
        _currentToken = token;
        _currentUserId = user.Id;
        SessionNotice = null;
        try
        {
            if (remember)
            {
                _tokenStore.Write(token);
            }
            else
            {
                _tokenStore.Clear();
            }
        }
        catch (Exception ex) when (ex is CryptographicException or IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            // Вхід залишається дійсним у пам'яті, навіть якщо ОС не дозволила зберегти токен.
            SessionNotice = "Сессия не сохранена на компьютере. При следующем запуске войдите снова.";
        }
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
