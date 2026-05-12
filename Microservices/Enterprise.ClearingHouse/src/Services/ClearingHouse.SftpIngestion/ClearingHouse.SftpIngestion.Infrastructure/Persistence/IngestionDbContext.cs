using ClearingHouse.SftpIngestion.Domain.Entities;
using ClearingHouse.SftpIngestion.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClearingHouse.SftpIngestion.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for the SFTP Ingestion service.
/// </summary>
public sealed class IngestionDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the SFTP ingestion jobs.
    /// </summary>
    public DbSet<SftpIngestionJob> IngestionJobs => Set<SftpIngestionJob>();

    /// <summary>
    /// Gets or sets the ingested files.
    /// </summary>
    public DbSet<IngestedFile> IngestedFiles => Set<IngestedFile>();

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    public IngestionDbContext(DbContextOptions<IngestionDbContext> options) : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SftpIngestionJob>(entity =>
        {
            entity.ToTable("SftpIngestionJobs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PollingSchedule)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(e => e.Version)
                .IsConcurrencyToken();

            entity.OwnsOne(e => e.ClearinghouseIdentifier, ci =>
            {
                ci.Property(c => c.Name).HasColumnName("ClearinghouseName").HasMaxLength(200).IsRequired();
                ci.Property(c => c.Code).HasColumnName("ClearinghouseCode").HasMaxLength(50).IsRequired();
                ci.Property(c => c.Type).HasColumnName("ClearinghouseType").HasConversion<string>().HasMaxLength(20);
            });

            entity.OwnsOne(e => e.CorrelationId, cid =>
            {
                cid.Property(c => c.Value).HasColumnName("CorrelationId").HasMaxLength(100).IsRequired();
            });

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<IngestedFile>(entity =>
        {
            entity.ToTable("IngestedFiles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FileName)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.ContentHash)
                .HasMaxLength(64);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.OwnsOne(e => e.EdiTransactionType, edi =>
            {
                edi.Property(t => t.Code).HasColumnName("EdiTransactionTypeCode").HasMaxLength(10).IsRequired();
                edi.Property(t => t.Description).HasColumnName("EdiTransactionTypeDescription").HasMaxLength(200);
            });

            entity.OwnsOne(e => e.FileReference, fr =>
            {
                fr.Property(f => f.ContainerName).HasColumnName("BlobContainerName").HasMaxLength(200);
                fr.Property(f => f.BlobPath).HasColumnName("BlobPath").HasMaxLength(1000);
                fr.Property(f => f.FileName).HasColumnName("BlobFileName").HasMaxLength(500);
                fr.Property(f => f.FileSize).HasColumnName("BlobFileSize");
                fr.Property(f => f.ContentHash).HasColumnName("BlobContentHash").HasMaxLength(64);
            });

            entity.OwnsOne(e => e.CorrelationId, cid =>
            {
                cid.Property(c => c.Value).HasColumnName("CorrelationId").HasMaxLength(100).IsRequired();
            });

            entity.HasIndex(e => e.IngestionJobId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IngestedAt);

            entity.Ignore(e => e.DomainEvents);
        });
    }
}
