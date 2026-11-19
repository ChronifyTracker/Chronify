using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Chronify.Services.Imports;

public record ExternalTaskSource(string Id, string Name)
{
    public override string ToString()
    {
        return Name;
    }
}
public record ExternalTask(
    string Provider,
    string Id,
    string Title,
    string Description,
    DateTime? DueDate,
    string Category,
    string Url
)
{
    public List<ExternalAttachment> Attachments { get; } = [];
}
public record ExternalAttachment(string Name, byte[] Content);

public interface IExternalTaskProvider
{
    Task<List<ExternalTaskSource>> Sources(CancellationToken cancellation);
    Task<List<ExternalTask>> Tasks(ExternalTaskSource source, CancellationToken cancellation);
    Task<string> Description(ExternalTask task, CancellationToken cancellation);
}

public static class ExternalHttp
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
    public static async Task<JsonElement> Json(
        HttpMethod method,
        string url,
        string token,
        object? body,
        CancellationToken cancellation,
        bool notion = false
    )
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (notion)
        {
            request.Headers.Add("Notion-Version", "2025-09-03");
        }

        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        using var response = await Http.SendAsync(request, cancellation);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(Texts.Format("Errors.ImportHttp", (notion ? "Notion" : "Classroom"), (int)response.StatusCode));
        }

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation));
        return json.RootElement.Clone();
    }
    public static string Text(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
    }

    public static string RichText(JsonElement values)
    {
        return string.Concat(values.EnumerateArray().Select(value =>
        value.TryGetProperty("plain_text", out var text) ? text.GetString()
        : value.TryGetProperty("text", out var item) ? Text(item, "content") : ""));
    }
}

public class TaskImportService(Guid userId, Func<AppDbContext>? createContext = null)
{
    private readonly Func<AppDbContext> _createContext = createContext ?? (() => new AppDbContext());
    public async Task<int> Import(
        IExternalTaskProvider provider,
        IEnumerable<ExternalTask> selected,
        CancellationToken cancellation
    )
    {
        var prepared = new List<ExternalTask>();
        using (var db = _createContext())
        {
            var existing = db.ExternalSources.Where(s => s.UserId == userId).Select(s => new { s.Provider, s.ExternalId })
                .ToList();
            foreach (var item in selected.DistinctBy(t => (t.Provider, t.Id)))
            {
                if (!existing.Any(s => s.Provider == item.Provider && s.ExternalId == item.Id))
                {
                    prepared.Add(item with { Description = await provider.Description(item, cancellation) });
                    if (prepared.Sum(t => t.Attachments.Sum(a => (long)a.Content.Length)) > 100 * 1024 * 1024)
                    {
                        throw new InvalidOperationException(Texts.Get("Errors.ImportTotalSize"));
                    }
                }
            }
        }
        cancellation.ThrowIfCancellationRequested();
        using var context = _createContext();
        using var transaction = context.Database.BeginTransaction();
        int count = 0;
        foreach (var item in prepared)
        {
            if (context.ExternalSources.Any(s => s.UserId == userId && s.Provider == item.Provider
                && s.ExternalId == item.Id))
            {
                continue;
            }

            var category = context.Categories.Local.FirstOrDefault(c => c.UserId == userId
                && c.Name == item.Category)
                            ?? context.Categories.FirstOrDefault(c => c.UserId == userId && c.Name == item.Category);
            if (category is null)
            {
                category = new Category(item.Category, "#F34F78") { UserId = userId };
                context.Categories.Add(category);
            }
            string description = item.Description + (item.Url.Length > 0 ? Texts.Get("Import.SourceSuffix") + item.Url : "");
            var task = TaskItem.FromImport(
                item.Title,
                description.Trim(),
                item.DueDate,
                category: category
            );
            task.UserId = userId;
            context.Tasks.Add(task);
            foreach (var attachment in item.Attachments)
            {
                context.TaskAttachments.Add(new TaskAttachment { UserId = userId, TaskId = task.Id, Name = attachment.Name, Content = attachment.Content });
            }

            context.ExternalSources.Add(new ExternalSourceInfo
            {
                UserId = userId,
                TaskId = task.Id,
                Provider = item.Provider,
                ExternalId = item.Id,
                Url = item.Url,
                SyncState = "Imported",
                LastSyncedAt = DateTime.UtcNow
            });
            count++;
        }
        context.SaveChanges();
        transaction.Commit();
        return count;
    }
}
