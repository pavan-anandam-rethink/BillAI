using Microsoft.EntityFrameworkCore;

namespace BillingService.App.Persistence;

public sealed class BillingOutboxDbContext : DbContext
{
    public BillingOutboxDbContext(DbContextOptions<BillingOutboxDbContext> options) : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var outbox = modelBuilder.Entity<OutboxMessage>();
        outbox.ToTable("BillingOutboxMessages");
        outbox.HasKey(x => x.Id);
        outbox.Property(x => x.EventType).HasMaxLength(300).IsRequired();
        outbox.Property(x => x.CorrelationId).HasMaxLength(128);
        outbox.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        outbox.HasIndex(x => new { x.ProcessedUtc, x.CreatedUtc });
    }
}

