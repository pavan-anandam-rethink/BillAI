using Microsoft.EntityFrameworkCore;
using FileMetadata.Domain.Entities;

namespace FileMetadata.Infrastructure.Persistence;

public class FileMetadataDbContext : DbContext
{
    public DbSet<FileMetadataRecord> FileMetadataRecords => Set<FileMetadataRecord>();
    public DbSet<FileEventHistory> FileEventHistories => Set<FileEventHistory>();

    public FileMetadataDbContext(DbContextOptions<FileMetadataDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileMetadataRecord>(entity =>
        {
            entity.ToTable("FileMetadataRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.BlobUri).HasMaxLength(2000);
            entity.Property(e => e.ContentHash).HasMaxLength(128);
            entity.Property(e => e.ClearinghouseName).HasMaxLength(100);
            entity.Property(e => e.CorrelationId).HasMaxLength(128);
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.ClearinghouseId);
        });

        modelBuilder.Entity<FileEventHistory>(entity =>
        {
            entity.ToTable("FileEventHistories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.CorrelationId).HasMaxLength(128);
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
            entity.HasIndex(e => e.FileId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
