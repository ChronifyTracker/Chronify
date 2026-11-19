using Chronify;
using Chronify.Services;
using Chronify.Teams;

// Замінює тільки мережеву межу. Усі локальні записи та транзакції перевіряються у справжній SQLite.
public sealed class FakeAccounts : IAccountGateway
{
    private readonly Dictionary<string, UserAccount> _users = new();
    private readonly HashSet<string> _sessions = [];
    public string Workspace => "test-workspace";

    public bool Offline { get; set; }
    public bool RejectRegistration { get; set; }
    public int Count => _users.Count;

    public bool IsSessionValid(string token)
    {
        return _sessions.Contains(token);
    }

    public Guid? Id(string email)
    {
        return _users.GetValueOrDefault(email.ToUpperInvariant())?.Id;
    }

    public Task<CloudSession> Register(CloudCredentials credentials)
    {
        CheckNetwork();
        string email = credentials.Email.ToUpperInvariant();
        if (RejectRegistration || _users.ContainsKey(email))
        {
            throw new InvalidOperationException("Registration rejected");
        }

        var hash = PasswordHasher.Create(credentials.Password);
        var user = new UserAccount { Email = email, DisplayName = credentials.Name, PasswordHash = hash.Hash, PasswordSalt = hash.Salt };
        _users.Add(email, user);
        return Task.FromResult(Session(user));
    }
    public Task<CloudSession> Login(CloudCredentials credentials, bool registerLegacyAccount)
    {
        CheckNetwork();
        var user = _users.GetValueOrDefault(credentials.Email.ToUpperInvariant());
        if (user is null && registerLegacyAccount)
        {
            return Register(credentials);
        }

        if (user is null || !PasswordHasher.Verify(credentials.Password, user))
        {
            throw new InvalidOperationException("Invalid credentials");
        }

        return Task.FromResult(Session(user));
    }
    public Task Logout(string token)
    {
        CheckNetwork();
        _sessions.Remove(token);
        return Task.CompletedTask;
    }
    private void CheckNetwork()
    {
        if (Offline)
        {
            throw new AccountUnavailableException("Offline fixture");
        }
    }
    private CloudSession Session(UserAccount user)
    {
        string token = Guid.NewGuid().ToString("N");
        _sessions.Add(token);
        return new(user.Id, token, user.DisplayName);
    }
}
