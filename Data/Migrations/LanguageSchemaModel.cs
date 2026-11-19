using Microsoft.EntityFrameworkCore;

namespace Chronify.Data.Migrations;

internal static class LanguageSchemaModel
{
    public static void Build(ModelBuilder modelBuilder)
    {
        OfflineSchemaModel.Build(modelBuilder);
        modelBuilder.Entity("Chronify.UserPreferences", entity =>
        {
            entity.Property<string>("LanguageCode").IsRequired().HasColumnType("TEXT");
        });
    }
}
