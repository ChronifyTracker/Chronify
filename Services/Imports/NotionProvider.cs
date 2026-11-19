using System.Globalization;
using System.Text.Json;

namespace Chronify.Services.Imports;

public class NotionProvider(Guid userId, Func<AppDbContext>? createContext = null) : IExternalTaskProvider
{
    private readonly ConnectionStore _connections = new(userId, createContext);
    private string Token => _connections.Secret("Notion");

    public async Task Connect(string token, CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Введите токен Notion с доступом к таблице Tasks.");
        }

        await ExternalHttp.Json(
            HttpMethod.Get,
            "https://api.notion.com/v1/users/me",
            token.Trim(),
            null,
            cancellation,
            true
        );
        _connections.Save("Notion", token.Trim());
    }

    public async Task<List<ExternalTaskSource>> Sources(CancellationToken cancellation)
    {
        var results = new List<ExternalTaskSource>();
        string next = "";
        do
        {
            var body = new Dictionary<string, object> { ["filter"] = new { property = "object", value = "data_source" }, ["page_size"] = 100 };
            if (next.Length > 0)
            {
                body["start_cursor"] = next;
            }

            var root = await ExternalHttp.Json(
                HttpMethod.Post,
                "https://api.notion.com/v1/search",
                Token,
                body,
                cancellation,
                true
            );
            foreach (var item in root.GetProperty("results").EnumerateArray())
            {
                results.Add(new(
                    ExternalHttp.Text(item, "id"),
                    item.TryGetProperty("title", out var title) ? ExternalHttp.RichText(title) : "Без названия"
                ));
            }

            next = ExternalHttp.Text(root, "next_cursor");
        } while (next.Length > 0);
        return results;
    }

    public async Task<List<ExternalTask>> Tasks(ExternalTaskSource source, CancellationToken cancellation)
    {
        var results = new List<ExternalTask>();
        string next = "";
        do
        {
            var body = new Dictionary<string, object> { ["page_size"] = 100 };
            if (next.Length > 0)
            {
                body["start_cursor"] = next;
            }

            var root = await ExternalHttp.Json(
                HttpMethod.Post,
                $"https://api.notion.com/v1/data_sources/{Uri.EscapeDataString(source.Id)}/query",
                Token,
                body,
                cancellation,
                true
            );
            foreach (var page in root.GetProperty("results").EnumerateArray())
            {
                results.Add(Parse(source, page));
            }

            next = ExternalHttp.Text(root, "next_cursor");
        } while (next.Length > 0);
        return results;
    }

    public static ExternalTask Parse(ExternalTaskSource source, JsonElement page)
    {
        string title = "";
        DateTime? due = null;
        // Перша колонка Notion — назва сторінки. Її вміст читаємо окремо як опис задачі.
        foreach (var property in page.GetProperty("properties").EnumerateObject())
        {
            string type = ExternalHttp.Text(property.Value, "type");
            if (type == "title")
            {
                title = ExternalHttp.RichText(property.Value.GetProperty("title"));
            }

            if (type == "date" && due is null
                && property.Value.GetProperty("date").ValueKind == JsonValueKind.Object)
            {
                string start = ExternalHttp.Text(property.Value.GetProperty("date"), "start");
                if (DateTimeOffset.TryParse(
                    start,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed
                ))
                {
                    due = start.Length == 10 ? parsed.Date.AddDays(1).AddSeconds(-1) : parsed.LocalDateTime;
                }
            }
        }
        return new(
            "Notion",
            ExternalHttp.Text(page, "id"),
            title.Length > 0 ? title : "Без названия",
            "",
            due,
            source.Name,
            ExternalHttp.Text(
            page,
            "url"
        )
        );
    }

    public Task<string> Description(ExternalTask task, CancellationToken cancellation)
    {
        task.Attachments.Clear();
        return ReadBlocks(
            task.Id,
            task.Attachments,
            cancellation,
            0
        );
    }

    private async Task<string> ReadBlocks(
        string id,
        List<ExternalAttachment> attachments,
        CancellationToken cancellation,
        int depth
    )
    {
        if (depth > 20)
        {
            throw new InvalidOperationException("Страница Notion имеет слишком большую вложенность. Импорт отменён.");
        }

        var lines = new List<string>();
        string next = "";
        do
        {
            string cursor = next.Length == 0 ? "" : "&start_cursor=" + Uri.EscapeDataString(next);
            var root = await ExternalHttp.Json(
                HttpMethod.Get,
                $"https://api.notion.com/v1/blocks/{Uri.EscapeDataString(id)}/children?page_size=100{cursor}",
                Token,
                null,
                cancellation,
                true
            );
            foreach (var block in root.GetProperty("results").EnumerateArray())
            {
                string type = ExternalHttp.Text(block, "type");
                if (block.TryGetProperty(type, out var value))
                {
                    if (value.TryGetProperty("rich_text", out var rich))
                    {
                        lines.Add(ExternalHttp.RichText(rich));
                    }

                    if (value.TryGetProperty("cells", out var cells))
                    {
                        lines.Add(string.Join(" | ", cells.EnumerateArray().Select(ExternalHttp.RichText)));
                    }

                    if (type is "file" or "pdf" or "image" or "video" or "audio")
                    {
                        string fileType = ExternalHttp.Text(value, "type");
                        if (value.TryGetProperty(fileType, out var file))
                        {
                            string url = ExternalHttp.Text(file, "url");
                            string name = ExternalHttp.Text(value, "name");
                            if (name.Length == 0 && Uri.TryCreate(url, UriKind.Absolute, out var fileUri))
                            {
                                name = Path.GetFileName(fileUri.LocalPath);
                            }

                            if (name.Length == 0)
                            {
                                name = "attachment";
                            }
                            // Токен Notion не передаємо файловому хостингу. Зберігаємо сам файл, а не тимчасове посилання.
                            attachments.Add(await DownloadFile(name, url, cancellation));
                            if (attachments.Sum(a => (long)a.Content.Length) > 50 * 1024 * 1024)
                            {
                                throw new InvalidOperationException("Вложения одной задачи превышают 50 МБ. Импорт отменён.");
                            }

                            lines.Add("Локальное вложение: " + name);
                        }
                    }
                    if (type is "bookmark" or "link_preview" or "embed")
                    {
                        lines.Add(ExternalHttp.Text(value, "url"));
                    }
                }
                if (block.TryGetProperty("has_children", out var children) && children.GetBoolean())
                {
                    lines.Add(await ReadBlocks(
                        ExternalHttp.Text(block, "id"),
                        attachments,
                        cancellation,
                        depth + 1
                    ));
                }
            }
            next = ExternalHttp.Text(root, "next_cursor");
        } while (next.Length > 0);
        return string.Join("\n", lines.Where(line => line.Length > 0));
    }

    private static async Task<ExternalAttachment> DownloadFile(
        string name,
        string url,
        CancellationToken cancellation
    )
    {
        const int limit = 20 * 1024 * 1024;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https")
        {
            throw new InvalidOperationException("Вложение Notion не имеет доступного HTTPS-адреса. Импорт отменён.");
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(40) };
        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellation);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > limit)
        {
            throw new InvalidOperationException("Размер вложения превышает 20 МБ. Импорт отменён.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellation);
        using var saved = new MemoryStream();
        byte[] buffer = new byte[81920];
        int count;
        while ((count = await stream.ReadAsync(buffer, cancellation)) > 0)
        {
            if (saved.Length + count > limit)
            {
                throw new InvalidOperationException("Размер вложения превышает 20 МБ. Импорт отменён.");
            }

            saved.Write(buffer, 0, count);
        }
        return new ExternalAttachment(name, saved.ToArray());
    }
}
