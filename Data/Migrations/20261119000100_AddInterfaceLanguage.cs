using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Chronify.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261119000100_AddInterfaceLanguage")]
public class AddInterfaceLanguage : Migration
{
    protected override void Up(MigrationBuilder migration)
    {
        // Наявні акаунти зберігають попередню мову інтерфейсу.
        migration.AddColumn<string>(
            name: "LanguageCode",
            table: "UserPreferences",
            type: "TEXT",
            nullable: false,
            defaultValue: "ru"
        );
    }

    protected override void Down(MigrationBuilder migration)
    {
        migration.DropColumn(name: "LanguageCode", table: "UserPreferences");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        LanguageSchemaModel.Build(modelBuilder);
    }
}
