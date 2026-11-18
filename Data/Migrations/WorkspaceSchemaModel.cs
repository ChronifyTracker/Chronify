using Microsoft.EntityFrameworkCore;

namespace Chronify.Data.Migrations;

internal static class WorkspaceSchemaModel
{
    public static void Build(ModelBuilder modelBuilder)
    {
        TaskLifecycleSchemaModel.Build(modelBuilder);
        modelBuilder.Entity("Chronify.TaskItem", entity =>
        {
            entity.Property<Guid?>("UserId").HasColumnType("TEXT");
            entity.HasIndex("UserId");
        });
        modelBuilder.Entity("Chronify.Category", entity =>
        {
            entity.Property<Guid?>("UserId").HasColumnType("TEXT");
            entity.HasIndex("UserId");
        });
        modelBuilder.Entity("Chronify.UserAccount", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            entity.Property<string>("DisplayName").IsRequired().HasColumnType("TEXT");
            entity.Property<string>("Email").IsRequired().HasColumnType("TEXT");
            entity.Property<string>("NormalizedEmail").IsRequired().HasColumnType("TEXT");
            entity.Property<string>("PasswordHash").IsRequired().HasColumnType("TEXT");
            entity.Property<string>("PasswordSalt").IsRequired().HasColumnType("TEXT");
            entity.Property<int>("PasswordIterations").HasColumnType("INTEGER");
            entity.Property<DateTime>("CreatedAt").HasColumnType("TEXT");
            entity.HasKey("Id");
            entity.ToTable("Users");
            entity.HasIndex("NormalizedEmail").IsUnique();
        });
        modelBuilder.Entity("Chronify.UserSession", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            entity.Property<Guid>("UserId").HasColumnType("TEXT");
            entity.Property<string>("TokenHash").IsRequired().HasColumnType("TEXT");
            entity.Property<DateTime>("ExpiresAtUtc").HasColumnType("TEXT");
            entity.HasKey("Id");
            entity.ToTable("UserSessions");
            entity.HasIndex("UserId");
            entity.HasIndex("TokenHash").IsUnique();
            entity.HasOne("Chronify.UserAccount").WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
        modelBuilder.Entity("Chronify.UserPreferences", entity =>
        {
            entity.Property<Guid>("UserId").HasColumnType("TEXT");
            entity.Property<int>("DailyLimitMinutes").HasColumnType("INTEGER");
            entity.Property<int>("PlanLimitMinutes").HasColumnType("INTEGER");
            entity.HasKey("UserId");
            entity.ToTable("UserPreferences");
            entity.HasOne("Chronify.UserAccount").WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
        modelBuilder.Entity("Chronify.WorkSession", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            entity.Property<Guid>("UserId").HasColumnType("TEXT");
            entity.Property<Guid>("TaskId").HasColumnType("TEXT");
            entity.Property<DateTime>("StartedAt").HasColumnType("TEXT");
            entity.Property<int>("Minutes").HasColumnType("INTEGER");
            entity.Property<string>("Note").IsRequired().HasColumnType("TEXT");
            entity.HasKey("Id");
            entity.ToTable("WorkSessions");
            entity.HasIndex("UserId");
            entity.HasIndex("TaskId");
            entity.HasOne("Chronify.UserAccount").WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            entity.HasOne("Chronify.TaskItem").WithMany().HasForeignKey("TaskId").OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
        modelBuilder.Entity("Chronify.FilterPreset", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            entity.Property<Guid>("UserId").HasColumnType("TEXT");
            entity.Property<string>("Name").IsRequired().HasColumnType("TEXT");
            entity.Property<string>("Search").HasColumnType("TEXT");
            entity.Property<int?>("Priority").HasColumnType("INTEGER");
            entity.Property<Guid?>("CategoryId").HasColumnType("TEXT");
            entity.Property<int?>("Status").HasColumnType("INTEGER");
            entity.Property<int>("DateFilter").HasColumnType("INTEGER");
            entity.Property<int?>("SortBy").HasColumnType("INTEGER");
            entity.Property<bool>("SortDescending").HasColumnType("INTEGER");
            entity.HasKey("Id");
            entity.ToTable("FilterPresets");
            entity.HasIndex("UserId");
            entity.HasIndex("CategoryId");
            entity.HasOne("Chronify.UserAccount").WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            entity.HasOne("Chronify.Category").WithMany().HasForeignKey("CategoryId").OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity("Chronify.ExternalSourceInfo", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            entity.Property<Guid>("UserId").HasColumnType("TEXT");
            entity.Property<Guid>("TaskId").HasColumnType("TEXT");
            entity.Property<string>("Provider").IsRequired().HasColumnType("TEXT");
            entity.Property<string>("ExternalId").IsRequired().HasColumnType("TEXT");
            entity.Property<string>("Url").IsRequired().HasColumnType("TEXT");
            entity.Property<string>("SyncState").IsRequired().HasColumnType("TEXT");
            entity.Property<DateTime?>("LastSyncedAt").HasColumnType("TEXT");
            entity.HasKey("Id");
            entity.ToTable("ExternalSources");
            entity.HasIndex("TaskId").IsUnique();
            entity.HasIndex("UserId", "Provider", "ExternalId").IsUnique();
            entity.HasOne("Chronify.UserAccount").WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            entity.HasOne("Chronify.TaskItem").WithMany().HasForeignKey("TaskId").OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
    }
}
