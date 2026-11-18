using Microsoft.EntityFrameworkCore;
using Chronify.Teams;

namespace Chronify.Data.Migrations;

internal static class OfflineSchemaModel
{
    public static void Build(ModelBuilder model)
    {
        WorkspaceSchemaModel.Build(model);
        AddOfflineTables(model);
        model.Entity("Chronify.TaskItem", e =>
        {
            e.Property<Guid?>("GroupAssignmentId").HasColumnType("TEXT");
            e.Property<string>("AssignedByName").IsRequired().HasColumnType("TEXT");
        });
    }

    // Однакова конфігурація для поточної БД та зафіксованої моделі міграції.
    public static void AddOfflineTables(ModelBuilder model)
    {
        model.Entity("Chronify.TaskAttachment", e =>
        {
            e.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            e.Property<Guid>("UserId").HasColumnType("TEXT");
            e.Property<Guid>("TaskId").HasColumnType("TEXT");
            e.Property<string>("Name").IsRequired().HasColumnType("TEXT");
            e.Property<byte[]>("Content").IsRequired().HasColumnType("BLOB");
            e.HasKey("Id");
            e.HasIndex("UserId");
            e.HasIndex("TaskId");
            e.ToTable("TaskAttachments");
            e.HasOne("Chronify.TaskItem", null).WithMany().HasForeignKey("TaskId").OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            e.HasOne("Chronify.UserAccount", null).WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
        model.Entity("Chronify.ServiceConnection", e =>
        {
            e.Property<Guid>("UserId").HasColumnType("TEXT");
            e.Property<string>("Provider").HasColumnType("TEXT");
            e.Property<string>("Address").IsRequired().HasColumnType("TEXT");
            e.Property<string>("ProtectedSecret").IsRequired().HasColumnType("TEXT");
            e.Property<Guid?>("RemoteUserId").HasColumnType("TEXT");
            e.HasKey("UserId", "Provider");
            e.ToTable("ServiceConnections");
            e.HasOne("Chronify.UserAccount", null).WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
        model.Entity("Chronify.CachedGroup", e =>
        {
            e.Property<Guid>("UserId").HasColumnType("TEXT");
            e.Property<Guid>("Id").HasColumnType("TEXT");
            e.Property<string>("Name").IsRequired().HasColumnType("TEXT");
            e.Property<string>("InviteCode").IsRequired().HasColumnType("TEXT");
            e.Property<TeamRole>("Role").HasColumnType("INTEGER");
            e.Property<DateTime>("UpdatedAt").HasColumnType("TEXT");
            e.HasKey("UserId", "Id");
            e.ToTable("CachedGroups");
        });
        model.Entity("Chronify.CachedMember", e =>
        {
            e.Property<Guid>("UserId").HasColumnType("TEXT");
            e.Property<Guid>("GroupId").HasColumnType("TEXT");
            e.Property<Guid>("Id").HasColumnType("TEXT");
            e.Property<string>("Name").IsRequired().HasColumnType("TEXT");
            e.Property<TeamRole>("Role").HasColumnType("INTEGER");
            e.HasKey("UserId", "GroupId", "Id");
            e.ToTable("CachedMembers");
        });
        model.Entity("Chronify.CachedAssignment", e =>
        {
            foreach (string property in new[] { "UserId", "Id", "GroupId", "AssigneeId", "AssignedById" })
            {
                e.Property<Guid>(property).HasColumnType("TEXT");
            }
            foreach (string property in new[] { "Title", "Description", "AssigneeName", "AssignedByName" })
            {
                e.Property<string>(property).IsRequired().HasColumnType("TEXT");
            }
            e.Property<DateTime?>("DueDate").HasColumnType("TEXT");
            foreach (string property in new[] { "EstimatedMinutes", "Priority", "Status" })
            {
                e.Property<int>(property).HasColumnType("INTEGER");
            }
            e.Property<bool>("PendingCompletion").HasColumnType("INTEGER");
            e.HasKey("UserId", "Id");
            e.ToTable("CachedAssignments");
        });
    }
}
