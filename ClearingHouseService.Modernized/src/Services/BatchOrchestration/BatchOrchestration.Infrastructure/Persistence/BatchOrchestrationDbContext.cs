using Microsoft.EntityFrameworkCore;
using BatchOrchestration.Domain.Entities;

namespace BatchOrchestration.Infrastructure.Persistence;

public class BatchOrchestrationDbContext : DbContext
{
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchItem> BatchItems => Set<BatchItem>();

    public BatchOrchestrationDbContext(DbContextOptions<BatchOrchestrationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Batch>(entity =>
        {
            entity.ToTable("Batches");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CorrelationId).HasMaxLength(128);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.Status);
            entity.HasMany(e => e.Items).WithOne().HasForeignKey(e => e.BatchId);
        });

        modelBuilder.Entity<BatchItem>(entity =>
        {
            entity.ToTable("BatchItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(500);
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
        });

        base.OnModelCreating(modelBuilder);
    }
}
