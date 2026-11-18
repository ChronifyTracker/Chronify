using Microsoft.EntityFrameworkCore;

namespace Chronify.Data.Migrations;

internal static class TaskLifecycleSchemaModel
{
    public static void Build(ModelBuilder modelBuilder)
    {
        InitialSqliteSchemaModel.Build(modelBuilder);
        modelBuilder.Entity("Chronify.TaskItem")
            .Property<DateTime?>("CompletedAt").HasColumnType("TEXT");
    }
}
