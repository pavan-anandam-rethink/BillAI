using Microsoft.EntityFrameworkCore;
using EdiProcessing.Domain.Entities;

namespace EdiProcessing.Infrastructure.Persistence;

public class EdiProcessingDbContext : DbContext
{
    public DbSet<EdiDocument> EdiDocuments => Set<EdiDocument>();
    public DbSet<EdiSegment> EdiSegments => Set<EdiSegment>();
    public DbSet<ProcessingResult> ProcessingResults => Set<ProcessingResult>();

    public EdiProcessingDbContext(DbContextOptions<EdiProcessingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EdiDocument>(entity =>
        {
            entity.ToTable("EdiDocuments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CorrelationId).HasMaxLength(128);
            entity.Property(e => e.ValidationErrors).HasMaxLength(4000);
            entity.HasIndex(e => e.FileId);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasMany(e => e.Segments).WithOne().HasForeignKey(e => e.DocumentId);
            entity.OwnsOne(e => e.TransactionSet);
        });

        modelBuilder.Entity<EdiSegment>(entity =>
        {
            entity.ToTable("EdiSegments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SegmentId).HasMaxLength(10);
            entity.Property(e => e.RawContent).HasMaxLength(4000);
        });

        modelBuilder.Entity<ProcessingResult>(entity =>
        {
            entity.ToTable("ProcessingResults");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CorrelationId).HasMaxLength(128);
            entity.HasIndex(e => e.FileId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
