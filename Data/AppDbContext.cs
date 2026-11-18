using Microsoft.EntityFrameworkCore;

namespace Chronify
{
    public class AppDbContext : DbContext
    {
        public DbSet<TaskItem> Tasks => Set<TaskItem>();

        public DbSet<Category> Categories => Set<Category>();

        public DbSet<UserAccount> Users => Set<UserAccount>();

        public DbSet<UserSession> UserSessions => Set<UserSession>();

        public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

        public DbSet<WorkSession> WorkSessions => Set<WorkSession>();

        public DbSet<FilterPreset> FilterPresets => Set<FilterPreset>();

        public DbSet<ExternalSourceInfo> ExternalSources => Set<ExternalSourceInfo>();

        public DbSet<ServiceConnection> ServiceConnections => Set<ServiceConnection>();

        public DbSet<CachedGroup> CachedGroups => Set<CachedGroup>();

        public DbSet<CachedMember> CachedMembers => Set<CachedMember>();

        public DbSet<CachedAssignment> CachedAssignments => Set<CachedAssignment>();

        public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();

        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public static void InitializeDatabase()
        {
            using var context = new AppDbContext();
            DatabaseInitializer.Initialize(context);
        }

        public static void CreateDatabaseIfMissing()
        {
            InitializeDatabase();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=chronify.db;Default Timeout=2");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Data.Migrations.OfflineSchemaModel.AddOfflineTables(modelBuilder);
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<TaskItem>().HasIndex(t => t.UserId);
            modelBuilder.Entity<Category>().HasIndex(c => c.UserId);
            modelBuilder.Entity<UserAccount>().HasIndex(u => u.NormalizedEmail).IsUnique();
            modelBuilder.Entity<UserSession>().HasIndex(s => s.TokenHash).IsUnique();
            modelBuilder.Entity<UserSession>().HasOne<UserAccount>().WithMany().HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserPreferences>().HasKey(p => p.UserId);
            modelBuilder.Entity<UserPreferences>().HasOne<UserAccount>().WithMany().HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WorkSession>().HasOne<UserAccount>().WithMany().HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WorkSession>().HasOne<TaskItem>().WithMany().HasForeignKey(s => s.TaskId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<FilterPreset>().HasOne<UserAccount>().WithMany().HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<FilterPreset>().HasOne<Category>().WithMany().HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<ExternalSourceInfo>().HasOne<UserAccount>().WithMany().HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ExternalSourceInfo>().HasOne<TaskItem>().WithMany().HasForeignKey(s => s.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ExternalSourceInfo>().HasIndex(s => s.TaskId).IsUnique();
            modelBuilder.Entity<ExternalSourceInfo>().HasIndex(s => new { s.UserId, s.Provider, s.ExternalId })
                .IsUnique();
        }
    }
}
