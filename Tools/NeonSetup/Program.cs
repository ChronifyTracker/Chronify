using System.Reflection;
using System.Security.Cryptography;
using Chronify.Services;
using Npgsql;

// Цю утиліту запускає розробник. Реквізити власника БД не використовуються самим застосунком.
string admin = NeonConfiguration.Read("DATABASE_URL_UNPOOLED") ?? NeonConfiguration.Read("DATABASE_URL")
    ?? throw new InvalidOperationException("Сначала выполните neon env pull.");
await using var owner = new NpgsqlConnection(NeonConfiguration.ConnectionString(admin));
await owner.OpenAsync();
Console.WriteLine("Neon connection: OK; TLS certificate verification enabled.");
if (args.Contains("--inspect"))
{
    await using var inspection = new NpgsqlCommand("SELECT current_database(), version()", owner);
    await using var reader = await inspection.ExecuteReaderAsync();
    await reader.ReadAsync();
    Console.WriteLine("Database: " + reader.GetString(0));
    Console.WriteLine(reader.GetString(1).Split(" on ")[0]);
    return;
}

string? previous = NeonConfiguration.Read("CHRONIFY_DATABASE_URL");
string password = previous is null ? Convert.ToHexString(RandomNumberGenerator.GetBytes(24))
    : new NpgsqlConnectionStringBuilder(NeonConfiguration.ConnectionString(previous)).Password!;
await using (var transaction = await owner.BeginTransactionAsync())
{
    string resource = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(n => n.EndsWith("schema.sql"));
    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)!;
    using var text = new StreamReader(stream);
    await using (var schema = new NpgsqlCommand(await text.ReadToEndAsync(), owner, transaction))
    {
        await schema.ExecuteNonQueryAsync();
    }

    await using (var exists = new NpgsqlCommand(
        "SELECT count(*) FROM pg_roles WHERE rolname='chronify_client'",
        owner,
        transaction
    ))
    {
        if ((long)(await exists.ExecuteScalarAsync())! == 0)
        {
            // Пароль згенеровано з hex-символів, тому тут немає довільного SQL-вводу.
            await using var role = new NpgsqlCommand(
                "CREATE ROLE chronify_client LOGIN PASSWORD '" + password + "'",
                owner,
                transaction
            );
            await role.ExecuteNonQueryAsync();
        }
        else if (previous is null)
        {
            throw new InvalidOperationException("chronify_client уже существует. Нужна прежняя конфигурация CHRONIFY_DATABASE_URL; пароль автоматически не меняется.");
        }
    }
    await using var permissions = new NpgsqlCommand("""
        REVOKE ALL ON ALL TABLES IN SCHEMA chronify FROM chronify_client;
        GRANT USAGE ON SCHEMA chronify TO chronify_client;
        GRANT EXECUTE ON FUNCTION chronify.request(text,text,jsonb) TO chronify_client;
        """, owner, transaction);
    await permissions.ExecuteNonQueryAsync();
    await transaction.CommitAsync();
}
var runtime = new NpgsqlConnectionStringBuilder(NeonConfiguration.ConnectionString(admin))
{
    Username = "chronify_client",
    Password = password
};
var lines = File.ReadAllLines(NeonConfiguration.EnvPath).Where(line => !line.StartsWith(
    "CHRONIFY_DATABASE_URL=",
    StringComparison.Ordinal
))
    .ToList();
lines.Add("CHRONIFY_DATABASE_URL='" + runtime.ConnectionString + "'");
await File.WriteAllLinesAsync(NeonConfiguration.EnvPath, lines);
Console.WriteLine("Schema and group permission function: installed.");
Console.WriteLine("Restricted client connection: saved to .env.local (secret hidden).");
await using var client = new NpgsqlConnection(runtime.ConnectionString);
await client.OpenAsync();
try
{
    await using var forbidden = new NpgsqlCommand("SELECT * FROM chronify.users", client);
    await forbidden.ExecuteScalarAsync();
    throw new InvalidOperationException("SECURITY CHECK FAILED: direct table access was allowed.");
}
catch (PostgresException ex) when (ex.SqlState == "42501")
{
    Console.WriteLine("Direct access to users table: DENIED, as expected.");
}
Console.WriteLine("Neon setup: OK.");
