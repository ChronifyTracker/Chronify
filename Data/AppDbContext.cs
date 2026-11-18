using Microsoft.EntityFrameworkCore;

namespace Chronify
{
    public class AppDbContext : DbContext
    {
        public DbSet<TaskItem> Tasks => Set<TaskItem>();

        public DbSet<Category> Categories => Set<Category>();

        public static void CreateDatabaseIfMissing()
        {
            using var context = new AppDbContext();
            context.Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=chronify.db;Default Timeout=2");
        }
    }
}
