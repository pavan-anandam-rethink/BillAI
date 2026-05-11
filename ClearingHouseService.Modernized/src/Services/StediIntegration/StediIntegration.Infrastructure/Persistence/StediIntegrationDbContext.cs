using Microsoft.EntityFrameworkCore;
using StediIntegration.Domain.Entities;

namespace StediIntegration.Infrastructure.Persistence;

public class StediIntegrationDbContext : DbContext
{
    public DbSet<StediTransaction> StediTransactions => Set<StediTransaction>();
    public DbSet<StediWebhookEvent> StediWebhookEvents => Set<StediWebhookEvent>();

    public StediIntegrationDbContext(DbContextOptions<StediIntegrationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StediTransaction>(entity =>
        {
            entity.ToTable("StediTransactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StediTransactionId).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Direction).HasMaxLength(20);
            entity.Property(e => e.CorrelationId).HasMaxLength(128);
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
            entity.HasIndex(e => e.StediTransactionId).IsUnique();
            entity.HasIndex(e => e.CorrelationId);
        });

        modelBuilder.Entity<StediWebhookEvent>(entity =>
        {
            entity.ToTable("StediWebhookEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WebhookId).HasMaxLength(200);
            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.CorrelationId).HasMaxLength(128);
            entity.HasIndex(e => e.WebhookId).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
