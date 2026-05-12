using Microsoft.EntityFrameworkCore;
using SftpIngestion.Domain.Entities;

namespace SftpIngestion.Infrastructure.Persistence;

public class SftpIngestionDbContext : DbContext
{
    public DbSet<InboundFile> InboundFiles => Set<InboundFile>();
    public DbSet<SftpConnection> SftpConnections => Set<SftpConnection>();

    public SftpIngestionDbContext(DbContextOptions<SftpIngestionDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboundFile>(entity =>
        {
            entity.ToTable("InboundFiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.SourcePath).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.ClearinghouseId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ContentHash).HasMaxLength(128);
            entity.Property(e => e.BlobUri).HasMaxLength(2000);
            entity.Property(e => e.CorrelationId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastError).HasMaxLength(4000);
            entity.HasIndex(e => new { e.ClearinghouseId, e.Status });
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.ContentHash);
            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<SftpConnection>(entity =>
        {
            entity.ToTable("SftpConnections");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClearinghouseId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Host).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(200).IsRequired();
            entity.Property(e => e.UploadDirectory).HasMaxLength(500);
            entity.Property(e => e.DownloadDirectory).HasMaxLength(500);
            entity.HasIndex(e => e.ClearinghouseId).IsUnique();
            entity.Ignore(e => e.DomainEvents);
        });
    }
}
