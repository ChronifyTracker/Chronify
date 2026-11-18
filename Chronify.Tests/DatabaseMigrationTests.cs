using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Chronify.Tests;

public class DatabaseMigrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExistingDatabaseKeepsTasksCategoriesAndCompletionTime(bool hasCompletion)
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "ChronifyMigrationChecks",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "legacy.db");
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={path}").Options;
        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            string completionColumn = hasCompletion ? ", CompletedAt TEXT NULL" : "";
            string completionValue = hasCompletion ? ", '2026-01-02 12:00:00'" : "";
            command.CommandText = $"""
                CREATE TABLE Categories (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Color TEXT NOT NULL);
                CREATE TABLE Tasks (Id TEXT PRIMARY KEY, Title TEXT NOT NULL, Description TEXT NOT NULL,
                    DueDate TEXT NOT NULL, EstimatedMinutes INTEGER NOT NULL, Priority INTEGER NOT NULL,
                    Status INTEGER NOT NULL, CreatedAt TEXT NOT NULL, CategoryId TEXT NULL {completionColumn});
                INSERT INTO Categories VALUES ('11111111-1111-1111-1111-111111111111','Study','#123456');
                INSERT INTO Tasks VALUES ('22222222-2222-2222-2222-222222222222','Saved task','Saved description',
                    '2026-01-03 12:00:00',30,3,2,'2026-01-01 12:00:00','11111111-1111-1111-1111-111111111111' {completionValue});
                """;
            command.ExecuteNonQuery();
        }
        using var context = new AppDbContext(options);
        DatabaseInitializer.Initialize(context);
        Assert.False(context.Database.HasPendingModelChanges());
        var task = context.Tasks.Include(t => t.Category).Single();
        Assert.Equal("Saved task", task.Title);
        Assert.Equal("Saved description", task.Description);
        Assert.Equal("Study", task.Category!.Name);
        Assert.Equal(hasCompletion ? new DateTime(
            2026,
            1,
            2,
            12,
            0,
            0
        ) : (DateTime?)null, task.CompletedAt);
        Assert.Single(Directory.GetFiles(directory, "*.before-migration-*.db"));
        DatabaseInitializer.Initialize(context);
        Assert.Single(Directory.GetFiles(directory, "*.before-migration-*.db"));
        context.Categories.Remove(task.Category);
        context.SaveChanges();
        Assert.Null(context.Tasks.AsNoTracking().Single().CategoryId);
    }
}
