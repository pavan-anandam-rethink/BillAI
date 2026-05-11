using Microsoft.EntityFrameworkCore;
using FileTracking.Domain.Entities;

namespace FileTracking.Infrastructure.Persistence;

public class FileTrackingDbContext : DbContext
{
    public DbSet<FileTrackingRecord> FileTrackingRecords => Set<FileTrackingRecord>();
    public DbSet<FileTimeline> FileTimelines => Set<FileTimeline>();

    public FileTrackingDbContext(DbContextOptions<FileTrackingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileTrackingRecord>(entity =>
        {
            entity.ToTable("FileTrackingRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(500);
            entity.Property(e => e.ClearinghouseName).HasMaxLength(100);
            entity.Property(e => e.CorrelationId).HasMaxLength(128);
            entity.Property(e => e.BlobUri).HasMaxLength(2000);
            entity.HasIndex(e => e.FileId).IsUnique();
            entity.HasIndex(e => e.CorrelationId);
            entity.HasMany(e => e.Timeline).WithOne().HasForeignKey(e => e.TrackingRecordId);
        });

        modelBuilder.Entity<FileTimeline>(entity =>
        {
            entity.ToTable("FileTimelines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(2000);
        });

        base.OnModelCreating(modelBuilder);
    }
}
