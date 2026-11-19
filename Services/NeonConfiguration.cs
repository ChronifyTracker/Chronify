using Npgsql;

namespace Chronify.Services;

public static class NeonConfiguration
{
    public static string EnvPath
    {
        get
        {
            foreach (string start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
            {
                for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
                {
                    foreach (string name in new[] { ".env.local", ".env" })
                    {
                        string path = Path.Combine(directory.FullName, name);
                        if (File.Exists(path))
                        {
                            return path;
                        }
                    }
                }
            }

            return Path.Combine(Environment.CurrentDirectory, ".env.local");
        }
    }

    public static string? Read(string key)
    {
        string? value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!File.Exists(EnvPath))
        {
            return null;
        }

        foreach (string line in File.ReadLines(EnvPath))
        {
            string text = line.Trim();
            if (!text.StartsWith(key + "=", StringComparison.Ordinal))
            {
                continue;
            }

            return text[(key.Length + 1)..].Trim().Trim('"', '\'');
        }
        return null;
    }

    public static string ConnectionString(string value)
    {
        if (!value.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
        {
            return new NpgsqlConnectionStringBuilder(value).ConnectionString;
        }

        var uri = new Uri(value);
        string[] credentials = uri.UserInfo.Split(':', 2);
        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : "",
            SslMode = SslMode.VerifyFull,
            ChannelBinding = ChannelBinding.Require,
            Timeout = 20,
            CommandTimeout = 30
        }.ConnectionString;
    }

    public static string RuntimeConnection
    {
        get
        {
            return ConnectionString(Read("CHRONIFY_DATABASE_URL")
        ?? throw new InvalidOperationException(Texts.Get("Errors.NeonConfiguration")));
        }
    }

    public static string Workspace
    {
        get
        {
            var builder = new NpgsqlConnectionStringBuilder(RuntimeConnection);
            return builder.Host + "/" + builder.Database;
        }
    }
}
