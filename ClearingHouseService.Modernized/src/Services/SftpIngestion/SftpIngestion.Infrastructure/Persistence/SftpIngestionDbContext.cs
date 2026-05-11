using Microsoft.EntityFrameworkCore;
using SftpIngestion.Domain.Entities;

namespace SftpIngestion.Infrastructure.Persistence;

public class SftpIngestionDbContext : DbContext
{
    public DbSet<IngestedFile> IngestedFiles => Set<IngestedFile>();
    public DbSet<SftpConnection> SftpConnections => Set<SftpConnection>();

    public SftpIngestionDbContext(DbContextOptions<SftpIngestionDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IngestedFile>(entity =>
        {
            entity.ToTable("IngestedFiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.BlobUri).HasMaxLength(2000);
            entity.Property(e => e.ContentHash).HasMaxLength(128);
            entity.Property(e => e.ClearinghouseName).HasMaxLength(100);
            entity.Property(e => e.CorrelationId).HasMaxLength(128);
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => new { e.ClearinghouseId, e.Status });
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<SftpConnection>(entity =>
        {
            entity.ToTable("SftpConnections");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Host).HasMaxLength(500).IsRequired();
            entity.Property(e => e.UserName).HasMaxLength(200);
            entity.Property(e => e.ClearinghouseName).HasMaxLength(100);
            entity.HasIndex(e => e.ClearinghouseId).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
