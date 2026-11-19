using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Chronify.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261118000300_AddOfflineGroups")]
public class AddOfflineGroups : Migration
{
    protected override void Up(MigrationBuilder migration)
    {
        migration.AddColumn<Guid>(
            "GroupAssignmentId",
            "Tasks",
            type: "TEXT",
            nullable: true
        );
        migration.AddColumn<string>(
            "AssignedByName",
            "Tasks",
            type: "TEXT",
            nullable: false,
            defaultValue: ""
        );
        migration.Sql("""
            CREATE TABLE TaskAttachments (
                Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, TaskId TEXT NOT NULL, Name TEXT NOT NULL, Content BLOB NOT NULL,
                FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                FOREIGN KEY(TaskId) REFERENCES Tasks(Id) ON DELETE CASCADE);
            CREATE INDEX IX_TaskAttachments_UserId ON TaskAttachments(UserId);
            CREATE INDEX IX_TaskAttachments_TaskId ON TaskAttachments(TaskId);
            CREATE TABLE ServiceConnections (
                UserId TEXT NOT NULL, Provider TEXT NOT NULL, Address TEXT NOT NULL,
                ProtectedSecret TEXT NOT NULL, RemoteUserId TEXT NULL, PRIMARY KEY(UserId, Provider),
                FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE);
            CREATE TABLE CachedGroups (
                UserId TEXT NOT NULL, Id TEXT NOT NULL, Name TEXT NOT NULL, InviteCode TEXT NOT NULL,
                Role INTEGER NOT NULL, UpdatedAt TEXT NOT NULL, PRIMARY KEY(UserId, Id));
            CREATE TABLE CachedMembers (
                UserId TEXT NOT NULL, GroupId TEXT NOT NULL, Id TEXT NOT NULL, Name TEXT NOT NULL,
                Role INTEGER NOT NULL, PRIMARY KEY(UserId, GroupId, Id));
            CREATE TABLE CachedAssignments (
                UserId TEXT NOT NULL, Id TEXT NOT NULL, GroupId TEXT NOT NULL,
                AssigneeId TEXT NOT NULL, AssignedById TEXT NOT NULL, AssigneeName TEXT NOT NULL, AssignedByName TEXT NOT NULL,
                Title TEXT NOT NULL, Description TEXT NOT NULL, DueDate TEXT NULL,
                EstimatedMinutes INTEGER NOT NULL, Priority INTEGER NOT NULL, Status INTEGER NOT NULL,
                PendingCompletion INTEGER NOT NULL, PRIMARY KEY(UserId, Id));
            """);
    }

    protected override void Down(MigrationBuilder migration)
    {
        foreach (string table in new[] { "TaskAttachments", "ServiceConnections", "CachedGroups", "CachedMembers", "CachedAssignments" })
        {
            migration.DropTable(table);
        }

        migration.DropColumn("GroupAssignmentId", "Tasks");
        migration.DropColumn("AssignedByName", "Tasks");
    }

    protected override void BuildTargetModel(ModelBuilder model)
    {
        OfflineSchemaModel.Build(model);
    }
}
