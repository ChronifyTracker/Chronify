using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Chronify.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261118000100_AddTaskCompletionTime")]
public sealed class AddTaskCompletionTime : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            "CompletedAt",
            "Tasks",
            type: "TEXT",
            nullable: true
        );
        migrationBuilder.Sql("""
            UPDATE Tasks SET CompletedAt = (SELECT CompletedAt FROM __Chronify_CompletionBackup WHERE Id = Tasks.Id)
            WHERE Id IN (SELECT Id FROM __Chronify_CompletionBackup);
            DROP TABLE __Chronify_CompletionBackup;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("CompletedAt", "Tasks");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        TaskLifecycleSchemaModel.Build(modelBuilder);
    }
}
