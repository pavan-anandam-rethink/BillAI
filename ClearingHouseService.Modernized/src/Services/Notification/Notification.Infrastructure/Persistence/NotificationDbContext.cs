using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationRecord>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasMaxLength(100);
            entity.Property(e => e.Severity).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.Message).HasMaxLength(4000);
            entity.Property(e => e.CorrelationId).HasMaxLength(128);
            entity.Property(e => e.Channel).HasMaxLength(100);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.IsSent);
        });

        modelBuilder.Entity<AlertRule>(entity =>
        {
            entity.ToTable("AlertRules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.Severity).HasMaxLength(50);
            entity.Property(e => e.Channel).HasMaxLength(100);
            entity.Property(e => e.Condition).HasMaxLength(2000);
            entity.Property(e => e.RecipientGroup).HasMaxLength(500);
        });

        base.OnModelCreating(modelBuilder);
    }
}
