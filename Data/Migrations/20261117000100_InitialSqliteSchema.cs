using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Chronify.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261117000100_InitialSqliteSchema")]
public sealed class InitialSqliteSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Підтримуємо як старі бази, створені через EnsureCreated, так і нові встановлення.
        // Перебудовуємо Tasks у транзакції міграції, щоб дозволити порожній дедлайн без втрати записів.
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS __Chronify_CompletionBackup (Id TEXT PRIMARY KEY, CompletedAt TEXT NULL);
            CREATE TABLE IF NOT EXISTS "Categories" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Categories" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Color" TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "Tasks" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Tasks" PRIMARY KEY,
                "Title" TEXT NOT NULL,
                "Description" TEXT NOT NULL,
                "DueDate" TEXT NULL,
                "EstimatedMinutes" INTEGER NOT NULL,
                "Priority" INTEGER NOT NULL,
                "Status" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "CategoryId" TEXT NULL,
                CONSTRAINT "FK_Tasks_Categories_CategoryId" FOREIGN KEY ("CategoryId")
                    REFERENCES "Categories" ("Id") ON DELETE SET NULL
            );
            CREATE TABLE "__Chronify_Tasks_Upgrade" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Tasks" PRIMARY KEY,
                "Title" TEXT NOT NULL,
                "Description" TEXT NOT NULL,
                "DueDate" TEXT NULL,
                "EstimatedMinutes" INTEGER NOT NULL,
                "Priority" INTEGER NOT NULL,
                "Status" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "CategoryId" TEXT NULL,
                CONSTRAINT "FK_Tasks_Categories_CategoryId" FOREIGN KEY ("CategoryId")
                    REFERENCES "Categories" ("Id") ON DELETE SET NULL
            );
            INSERT INTO "__Chronify_Tasks_Upgrade"
                ("Id", "Title", "Description", "DueDate", "EstimatedMinutes", "Priority", "Status", "CreatedAt", "CategoryId")
            SELECT "Id", "Title", "Description", "DueDate", "EstimatedMinutes", "Priority", "Status", "CreatedAt", "CategoryId"
            FROM "Tasks";
            DROP TABLE "Tasks";
            ALTER TABLE "__Chronify_Tasks_Upgrade" RENAME TO "Tasks";
            CREATE INDEX "IX_Tasks_CategoryId" ON "Tasks" ("CategoryId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Tasks");
        migrationBuilder.DropTable("Categories");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        InitialSqliteSchemaModel.Build(modelBuilder);
    }
}
