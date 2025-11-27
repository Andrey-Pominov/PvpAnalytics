using LoggingService.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoggingService.Infrastructure;

public class LoggingDbContext(DbContextOptions<LoggingDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationLog> ApplicationLogs { get; set; }
    public DbSet<RegisteredService> RegisteredServices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Level).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ServiceName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.Exception).HasColumnType("text");
            entity.Property(e => e.Properties).HasColumnType("text");

            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.ServiceName);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<RegisteredService>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ServiceName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Endpoint).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Version).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.RegisteredAt).IsRequired();
            entity.Property(e => e.LastHeartbeat).IsRequired();

            entity.HasIndex(e => e.ServiceName);
            entity.HasIndex(e => e.Status);
        });
    }
}

