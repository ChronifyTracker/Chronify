using Microsoft.EntityFrameworkCore;

namespace Chronify.Data.Migrations;

// Модель початкової міграції зафіксована; наступні міграції мають власні знімки схеми.
internal static class InitialSqliteSchemaModel
{
    public static void Build(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.14");

        modelBuilder.Entity("Chronify.Category", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            entity.Property<string>("Name").IsRequired().HasColumnType("TEXT");
            entity.Property<string>("Color").IsRequired().HasColumnType("TEXT");
            entity.HasKey("Id");
            entity.ToTable("Categories");
        });

        modelBuilder.Entity("Chronify.TaskItem", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            entity.Property<string>("Title").IsRequired().HasColumnType("TEXT");
            entity.Property<string>("Description").IsRequired().HasColumnType("TEXT");
            entity.Property<DateTime?>("DueDate").HasColumnType("TEXT");
            entity.Property<int>("EstimatedMinutes").HasColumnType("INTEGER");
            entity.Property<int>("Priority").HasColumnType("INTEGER");
            entity.Property<int>("Status").HasColumnType("INTEGER");
            entity.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            entity.Property<Guid?>("CategoryId").HasColumnType("TEXT");
            entity.HasKey("Id");
            entity.HasIndex("CategoryId");
            entity.ToTable("Tasks");
        });

        modelBuilder.Entity("Chronify.TaskItem", entity =>
        {
            entity.HasOne("Chronify.Category", "Category")
                .WithMany().HasForeignKey("CategoryId").OnDelete(DeleteBehavior.SetNull);
            entity.Navigation("Category");
        });
    }
}
