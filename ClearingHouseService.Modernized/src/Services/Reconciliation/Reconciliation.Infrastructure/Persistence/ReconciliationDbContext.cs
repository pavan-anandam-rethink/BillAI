using Microsoft.EntityFrameworkCore;
using Reconciliation.Domain.Entities;

namespace Reconciliation.Infrastructure.Persistence;

public class ReconciliationDbContext : DbContext
{
    public DbSet<ReconciliationRecord> ReconciliationRecords => Set<ReconciliationRecord>();
    public DbSet<PaymentReconciliation> PaymentReconciliations => Set<PaymentReconciliation>();

    public ReconciliationDbContext(DbContextOptions<ReconciliationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReconciliationRecord>(entity =>
        {
            entity.ToTable("ReconciliationRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClaimId).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.CorrelationId).HasMaxLength(128);
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
            entity.HasIndex(e => e.ClaimId);
            entity.HasIndex(e => e.CorrelationId);
        });

        modelBuilder.Entity<PaymentReconciliation>(entity =>
        {
            entity.ToTable("PaymentReconciliations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClaimId).HasMaxLength(100);
            entity.Property(e => e.PayerClaimId).HasMaxLength(100);
            entity.Property(e => e.PaymentStatus).HasMaxLength(50);
            entity.Property(e => e.CorrelationId).HasMaxLength(128);
            entity.Property(e => e.ClaimedAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            entity.Property(e => e.AdjustmentAmount).HasPrecision(18, 2);
        });

        base.OnModelCreating(modelBuilder);
    }
}
