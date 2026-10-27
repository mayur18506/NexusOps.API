using Microsoft.EntityFrameworkCore;
using NexusOps.API.Entities;

namespace NexusOps.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<MonitoredService> MonitoredServices { get; set; }
        public DbSet<HealthLog> HealthLogs { get; set; }
        public DbSet<Incident> Incidents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Fast dashboard queries ke liye Indexing
            modelBuilder.Entity<HealthLog>()
                .HasIndex(h => new { h.ServiceId, h.CheckedAt });

            modelBuilder.Entity<Incident>()
                .HasIndex(i => i.Status);

            // Cascade delete setup
            modelBuilder.Entity<MonitoredService>()
                .HasMany(s => s.HealthLogs)
                .WithOne(h => h.MonitoredService)
                .HasForeignKey(h => h.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}