using ClearingHouse.EdiProcessing.Domain.Entities;
using ClearingHouse.EdiProcessing.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClearingHouse.EdiProcessing.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for EDI processing entities.
/// </summary>
public sealed class EdiProcessingDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EdiProcessingDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public EdiProcessingDbContext(DbContextOptions<EdiProcessingDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets or sets the EDI files.</summary>
    public DbSet<EdiFile> EdiFiles => Set<EdiFile>();

    /// <summary>Gets or sets the EDI segments.</summary>
    public DbSet<EdiSegment> EdiSegments => Set<EdiSegment>();

    /// <summary>Gets or sets the EDI processing errors.</summary>
    public DbSet<EdiProcessingError> EdiProcessingErrors => Set<EdiProcessingError>();

    /// <summary>Gets or sets the claim transactions.</summary>
    public DbSet<ClaimTransaction> ClaimTransactions => Set<ClaimTransaction>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureEdiFile(modelBuilder);
        ConfigureEdiSegment(modelBuilder);
        ConfigureEdiProcessingError(modelBuilder);
        ConfigureClaimTransaction(modelBuilder);
    }

    private static void ConfigureEdiFile(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EdiFile>(entity =>
        {
            entity.ToTable("EdiFiles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<EdiProcessingStatus>(v));

            entity.Property(e => e.CorrelationId)
                .HasMaxLength(100);

            entity.Property(e => e.BlobUri)
                .HasMaxLength(2000);

            entity.Property(e => e.TransactionType)
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.CompletedAt);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.CreatedAt);

            entity.Ignore(e => e.DomainEvents);
        });
    }

    private static void ConfigureEdiSegment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EdiSegment>(entity =>
        {
            entity.ToTable("EdiSegments");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            entity.Property(e => e.FileId)
                .IsRequired();

            entity.Property(e => e.SegmentIdentifier)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.RawData)
                .IsRequired();

            entity.Property(e => e.SequenceNumber)
                .IsRequired();

            entity.Property(e => e.IsValid);

            entity.HasIndex(e => e.FileId);
            entity.HasIndex(e => new { e.FileId, e.SequenceNumber });

            entity.Ignore(e => e.DomainEvents);
        });
    }

    private static void ConfigureEdiProcessingError(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EdiProcessingError>(entity =>
        {
            entity.ToTable("EdiProcessingErrors");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            entity.Property(e => e.FileId)
                .IsRequired();

            entity.Property(e => e.SegmentId);

            entity.Property(e => e.ErrorCode)
                .HasMaxLength(50);

            entity.Property(e => e.ErrorMessage)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(e => e.Severity)
                .IsRequired()
                .HasMaxLength(50)
                .HasConversion(
                    v => v.ToString(),
                    v => Enum.Parse<ValidationSeverity>(v));

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasIndex(e => e.FileId);
            entity.HasIndex(e => e.SegmentId);
            entity.HasIndex(e => e.Severity);

            entity.Ignore(e => e.DomainEvents);
        });
    }

    private static void ConfigureClaimTransaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClaimTransaction>(entity =>
        {
            entity.ToTable("ClaimTransactions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            entity.Property(e => e.FileId)
                .IsRequired();

            entity.Property(e => e.ClaimId)
                .HasMaxLength(100);

            entity.Property(e => e.PatientControlNumber)
                .HasMaxLength(100);

            entity.Property(e => e.TotalChargeAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.TransactionType)
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasIndex(e => e.FileId);
            entity.HasIndex(e => e.ClaimId);
            entity.HasIndex(e => e.PatientControlNumber);

            entity.Ignore(e => e.DomainEvents);
        });
    }
}
