using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Chronify;

public static class DatabaseInitializer
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.OpenConnection();
        try
        {
            var pending = context.Database.GetPendingMigrations().ToList();
            if (pending.Count > 0)
            {
                BackupExistingDatabase(context);
            }

            if (pending.Contains("20261117000100_InitialSqliteSchema"))
            {
                PreserveExistingCompletionTimes(context);
            }

            context.Database.Migrate();
        }
        finally
        {
            context.Database.CloseConnection();
        }
    }

    private static void PreserveExistingCompletionTimes(AppDbContext context)
    {
        var connection = (SqliteConnection)context.Database.GetDbConnection();
        using var columns = connection.CreateCommand();
        columns.CommandText = "PRAGMA table_info(Tasks)";
        bool hasCompletion = false;
        using (var reader = columns.ExecuteReader())
        {
            while (reader.Read())
            {
                hasCompletion |= reader.GetString(1) == "CompletedAt";
            }
        }

        if (!hasCompletion)
        {
            return;
        }
        // EnsureCreated вже міг створити CompletedAt. Зберігаємо його перед перебудовою Tasks.
        using var preserve = connection.CreateCommand();
        preserve.CommandText = """
            CREATE TABLE IF NOT EXISTS __Chronify_CompletionBackup (Id TEXT PRIMARY KEY, CompletedAt TEXT NULL);
            INSERT OR REPLACE INTO __Chronify_CompletionBackup SELECT Id, CompletedAt FROM Tasks;
            """;
        preserve.ExecuteNonQuery();
    }

    private static void BackupExistingDatabase(AppDbContext context)
    {
        var connection = (SqliteConnection)context.Database.GetDbConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Tasks'";
        if (Convert.ToInt64(command.ExecuteScalar()) == 0 || !File.Exists(connection.DataSource))
        {
            return;
        }

        // Резервна копія SQLite включає підтверджені записи з WAL, на відміну від копіювання лише файлу .db.
        var backupPath = connection.DataSource + $".before-migration-{DateTime.UtcNow:yyyyMMddHHmmssfff}.db";
        using var backup = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = backupPath,
            Pooling = false
        }.ToString());
        backup.Open();
        connection.BackupDatabase(backup);
    }
}
