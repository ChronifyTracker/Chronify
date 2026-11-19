using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;

namespace Chronify.Services.Imports;

public class ClassroomProvider(Guid userId, Func<AppDbContext>? createContext = null) : IExternalTaskProvider
{
    private readonly ConnectionStore _connections = new(userId, createContext);
    private static readonly string[] Scopes =
    [
        "https://www.googleapis.com/auth/classroom.courses.readonly",
        "https://www.googleapis.com/auth/classroom.coursework.me.readonly"
    ];
    private UserCredential? _credential;

    public async Task Connect(
        string clientId,
        string clientSecret,
        CancellationToken cancellation
    )
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new ArgumentException("Укажите Client ID и Client secret OAuth-приложения Google типа Desktop app.");
        }

        var flow = new PkceGoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId.Trim(), ClientSecret = clientSecret.Trim() },
            Scopes = Scopes
        });
        _credential = await new AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver()).AuthorizeAsync(
            userId.ToString(),
            cancellation
        );
        Save(clientId.Trim(), clientSecret.Trim(), _credential.Token);
    }

    private void Save(
        string clientId,
        string clientSecret,
        TokenResponse token
    )
    {
        _connections.Save(
            "Classroom",
            JsonSerializer.Serialize(new GoogleConnection(clientId, clientSecret, token))
        );
    }

    private async Task<string> Token(CancellationToken cancellation)
    {
        var saved = JsonSerializer.Deserialize<GoogleConnection>(_connections.Secret("Classroom"))!;
        _credential ??= new UserCredential(new PkceGoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = saved.ClientId, ClientSecret = saved.ClientSecret },
            Scopes = Scopes
        }), userId.ToString(), saved.Token);
        string token = await _credential.GetAccessTokenForRequestAsync(cancellationToken: cancellation);
        Save(saved.ClientId, saved.ClientSecret, _credential.Token);
        return token;
    }

    public async Task<List<ExternalTaskSource>> Sources(CancellationToken cancellation)
    {
        var results = new List<ExternalTaskSource>();
        string next = "", token = await Token(cancellation);
        do
        {
            var root = await ExternalHttp.Json(
                HttpMethod.Get,
                "https://classroom.googleapis.com/v1/courses?studentId=me&courseStates=ACTIVE&pageSize=100&pageToken=" + Uri.EscapeDataString(next),
                token,
                null,
                cancellation
            );
            if (root.TryGetProperty("courses", out var courses))
            {
                foreach (var course in courses.EnumerateArray())
                {
                    results.Add(new(ExternalHttp.Text(course, "id"), ExternalHttp.Text(course, "name")));
                }
            }

            next = ExternalHttp.Text(root, "nextPageToken");
        } while (next.Length > 0);
        return results;
    }

    public async Task<List<ExternalTask>> Tasks(ExternalTaskSource source, CancellationToken cancellation)
    {
        var results = new List<ExternalTask>();
        string next = "", token = await Token(cancellation);
        do
        {
            var root = await ExternalHttp.Json(
                HttpMethod.Get,
                $"https://classroom.googleapis.com/v1/courses/{Uri.EscapeDataString(source.Id)}/courseWork?pageSize=100&pageToken={Uri.EscapeDataString(next)}",
                token,
                null,
                cancellation
            );
            if (root.TryGetProperty("courseWork", out var tasks))
            {
                foreach (var task in tasks.EnumerateArray())
                {
                    results.Add(Parse(source, task));
                }
            }

            next = ExternalHttp.Text(root, "nextPageToken");
        } while (next.Length > 0);
        return results;
    }

    public static ExternalTask Parse(ExternalTaskSource source, JsonElement task)
    {
        DateTime? due = null;
        if (task.TryGetProperty("dueDate", out var date))
        {
            int hours = 23, minutes = 59;
            if (task.TryGetProperty("dueTime", out var time))
            {
                hours = time.TryGetProperty("hours", out var hour) ? hour.GetInt32() : 0;
                minutes = time.TryGetProperty("minutes", out var minute) ? minute.GetInt32() : 0;
            }
            due = new DateTime(
                date.GetProperty("year").GetInt32(),
                date.GetProperty("month").GetInt32(),
                date.GetProperty("day")
                .GetInt32(),
                hours,
                minutes,
                0,
                DateTimeKind.Utc
            ).ToLocalTime();
        }
        string description = ExternalHttp.Text(task, "description");
        if (task.TryGetProperty("materials", out var materials))
        {
            foreach (var material in materials.EnumerateArray())
            {
                foreach (var property in material.EnumerateObject())
                {
                    var value = property.Value;
                    if (value.TryGetProperty("driveFile", out var driveFile))
                    {
                        value = driveFile;
                    }

                    string url = ExternalHttp.Text(value, "alternateLink");
                    if (url.Length == 0)
                    {
                        url = ExternalHttp.Text(value, "url");
                    }

                    description += "\n" + ExternalHttp.Text(value, "title") + " " + url;
                }
            }
        }

        return new(
            "Classroom",
            source.Id + ":" + ExternalHttp.Text(task, "id"),
            ExternalHttp.Text(task, "title"),
            description.Trim(),
            due,
            source.Name,
            ExternalHttp.Text(task, "alternateLink")
        );
    }

    public Task<string> Description(ExternalTask task, CancellationToken cancellation)
    {
        return Task.FromResult(task.Description);
    }

    private record GoogleConnection(
        string ClientId,
        string ClientSecret,
        TokenResponse Token
    );
}
