using Microsoft.EntityFrameworkCore;
using BlobManagement.Domain.Entities;

namespace BlobManagement.Infrastructure.Persistence;

public class BlobManagementDbContext : DbContext
{
    public DbSet<BlobFile> BlobFiles => Set<BlobFile>();
    public DbSet<BlobContainer> BlobContainers => Set<BlobContainer>();

    public BlobManagementDbContext(DbContextOptions<BlobManagementDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlobFile>(entity =>
        {
            entity.ToTable("BlobFiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ContainerName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.BlobName).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.ContentHash).HasMaxLength(128);
            entity.Property(e => e.ContentType).HasMaxLength(200);
            entity.Property(e => e.RetentionPolicy).HasMaxLength(100);
            entity.HasIndex(e => new { e.ContainerName, e.BlobName });
        });

        modelBuilder.Entity<BlobContainer>(entity =>
        {
            entity.ToTable("BlobContainers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
