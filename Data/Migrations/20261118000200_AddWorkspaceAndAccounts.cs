using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Chronify.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261118000200_AddWorkspaceAndAccounts")]
public sealed class AddWorkspaceAndAccounts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            "UserId",
            "Tasks",
            type: "TEXT",
            nullable: true
        );
        migrationBuilder.AddColumn<Guid>(
            "UserId",
            "Categories",
            type: "TEXT",
            nullable: true
        );
        migrationBuilder.CreateIndex("IX_Tasks_UserId", "Tasks", "UserId");
        migrationBuilder.CreateIndex("IX_Categories_UserId", "Categories", "UserId");
        migrationBuilder.Sql("""
            CREATE TABLE "Users" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "DisplayName" TEXT NOT NULL,
                "Email" TEXT NOT NULL,
                "NormalizedEmail" TEXT NOT NULL,
                "PasswordHash" TEXT NOT NULL,
                "PasswordSalt" TEXT NOT NULL,
                "PasswordIterations" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            CREATE UNIQUE INDEX "IX_Users_NormalizedEmail" ON "Users" ("NormalizedEmail");
            CREATE TABLE "UserSessions" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "UserId" TEXT NOT NULL,
                "TokenHash" TEXT NOT NULL,
                "ExpiresAtUtc" TEXT NOT NULL,
                FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX "IX_UserSessions_UserId" ON "UserSessions" ("UserId");
            CREATE UNIQUE INDEX "IX_UserSessions_TokenHash" ON "UserSessions" ("TokenHash");
            CREATE TABLE "UserPreferences" (
                "UserId" TEXT NOT NULL PRIMARY KEY,
                "DailyLimitMinutes" INTEGER NOT NULL,
                "PlanLimitMinutes" INTEGER NOT NULL,
                FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
            );
            CREATE TABLE "WorkSessions" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "UserId" TEXT NOT NULL,
                "TaskId" TEXT NOT NULL,
                "StartedAt" TEXT NOT NULL,
                "Minutes" INTEGER NOT NULL,
                "Note" TEXT NOT NULL,
                FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
                FOREIGN KEY ("TaskId") REFERENCES "Tasks" ("Id") ON DELETE CASCADE
            );
            CREATE INDEX "IX_WorkSessions_UserId" ON "WorkSessions" ("UserId");
            CREATE INDEX "IX_WorkSessions_TaskId" ON "WorkSessions" ("TaskId");
            CREATE TABLE "FilterPresets" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "UserId" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "Search" TEXT NULL,
                "Priority" INTEGER NULL,
                "CategoryId" TEXT NULL,
                "Status" INTEGER NULL,
                "DateFilter" INTEGER NOT NULL,
                "SortBy" INTEGER NULL,
                "SortDescending" INTEGER NOT NULL,
                FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
                FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE SET NULL
            );
            CREATE INDEX "IX_FilterPresets_UserId" ON "FilterPresets" ("UserId");
            CREATE INDEX "IX_FilterPresets_CategoryId" ON "FilterPresets" ("CategoryId");
            CREATE TABLE "ExternalSources" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "UserId" TEXT NOT NULL,
                "TaskId" TEXT NOT NULL,
                "Provider" TEXT NOT NULL,
                "ExternalId" TEXT NOT NULL,
                "Url" TEXT NOT NULL,
                "SyncState" TEXT NOT NULL,
                "LastSyncedAt" TEXT NULL,
                FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
                FOREIGN KEY ("TaskId") REFERENCES "Tasks" ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX "IX_ExternalSources_TaskId" ON "ExternalSources" ("TaskId");
            CREATE UNIQUE INDEX "IX_ExternalSources_UserId_Provider_ExternalId" ON "ExternalSources" ("UserId", "Provider", "ExternalId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("WorkSessions");
        migrationBuilder.DropTable("FilterPresets");
        migrationBuilder.DropTable("ExternalSources");
        migrationBuilder.DropTable("UserSessions");
        migrationBuilder.DropTable("UserPreferences");
        migrationBuilder.DropTable("Users");
        migrationBuilder.DropIndex("IX_Tasks_UserId", "Tasks");
        migrationBuilder.DropIndex("IX_Categories_UserId", "Categories");
        migrationBuilder.DropColumn("UserId", "Tasks");
        migrationBuilder.DropColumn("UserId", "Categories");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        WorkspaceSchemaModel.Build(modelBuilder);
    }
}
