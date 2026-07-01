using ClaimLifecycleMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimLifecycleMonitoring.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="ClaimLifecycleEvent"/>.</summary>
public sealed class ClaimLifecycleEventConfiguration : IEntityTypeConfiguration<ClaimLifecycleEvent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ClaimLifecycleEvent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ClaimLifecycleEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ClaimId).IsRequired();
        builder.Property(x => x.Stage).HasConversion<int>().IsRequired();
        builder.Property(x => x.Outcome).HasConversion<int>().IsRequired();
        builder.Property(x => x.Message).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.ExceptionDetail).HasMaxLength(4000);
        builder.Property(x => x.OccurredUtc).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.ClaimId, x.OccurredUtc });
    }
}

/// <summary>EF Core configuration for <see cref="ClaimAlert"/>.</summary>
public sealed class ClaimAlertConfiguration : IEntityTypeConfiguration<ClaimAlert>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ClaimAlert> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ClaimAlerts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ClaimId).IsRequired();
        builder.Property(x => x.Category).HasConversion<int>().IsRequired();
        builder.Property(x => x.Severity).HasConversion<int>().IsRequired();
        builder.Property(x => x.StageAtAlert).HasConversion<int>().IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.AcknowledgedBy).HasMaxLength(128);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.ClaimId, x.Category, x.RaisedUtc });
        builder.HasIndex(x => x.Acknowledged);
    }
}

/// <summary>EF Core configuration for <see cref="StageSlaConfiguration"/>.</summary>
public sealed class StageSlaConfigurationConfiguration : IEntityTypeConfiguration<StageSlaConfiguration>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StageSlaConfiguration> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("StageSlaConfigurations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Stage).HasConversion<int>().IsRequired();
        builder.Property(x => x.BreachCategory).HasConversion<int>().IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.Stage).IsUnique();
    }
}
