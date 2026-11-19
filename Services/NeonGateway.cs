using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace Chronify.Services;

// Доступний лише виклик цієї функції. Клієнтський SQL-користувач не має доступу до таблиць.
public static class NeonGateway
{
    public static async Task<T> Request<T>(
        string action,
        object? data = null,
        string? token = null
    )
    {
        await using var connection = new NpgsqlConnection(NeonConfiguration.RuntimeConnection);
        await connection.OpenAsync();
        return await Request<T>(
            connection,
            action,
            data,
            token
        );
    }

    public static async Task<T> Request<T>(
        NpgsqlConnection connection,
        string action,
        object? data = null,
        string? token = null
    )
    {
        await using var command = new NpgsqlCommand(
            "SELECT chronify.request(@token, @action, @payload)::text",
            connection
        );
        command.Parameters.AddWithValue("token", token ?? "");
        command.Parameters.AddWithValue("action", action);
        command.Parameters.AddWithValue(
            "payload",
            NpgsqlDbType.Jsonb,
            JsonSerializer.Serialize(
            data ?? new { },
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        )
        );
        try
        {
            string result = (string)(await command.ExecuteScalarAsync())!;
            return JsonSerializer.Deserialize<T>(result, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        }
        catch (PostgresException ex) when (ex.SqlState == "P0001")
        {
            throw new InvalidOperationException(ServerErrors.Translate(ex.MessageText), ex);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            throw new InvalidOperationException(
                                                                         Texts.Get("Errors.RecordExists"),
                                                                         ex
                                                                     );
        }
    }
}
