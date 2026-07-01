using ClaimLifecycleMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimLifecycleMonitoring.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="Claim"/>.</summary>
public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Claims");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ClaimNumber).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.ClaimNumber).IsUnique();
        builder.Property(x => x.CustomerCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.AccountCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AccountName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.PatientId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PatientName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ProviderId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProviderName).HasMaxLength(256).IsRequired();

        builder.Property(x => x.CurrentStage).HasConversion<int>().IsRequired();
        builder.Property(x => x.CurrentStatus).HasConversion<int>().IsRequired();
        builder.Property(x => x.LastSuccessfulStage).HasConversion<int?>();
        builder.Property(x => x.NextExpectedStage).HasConversion<int?>();
        builder.Property(x => x.FailureCategory).HasConversion<int>().IsRequired();

        builder.Property(x => x.ExceptionMessage).HasMaxLength(4000);
        builder.Property(x => x.BilledAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.CustomerCode, x.AccountCode });
        builder.HasIndex(x => x.CurrentStage);
        builder.HasIndex(x => x.CurrentStatus);
        builder.HasIndex(x => x.ManualInterventionRequired);

        builder.HasMany(x => x.History)
            .WithOne()
            .HasForeignKey(x => x.ClaimId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent!.SetField("_history");

        builder.HasMany(x => x.Alerts)
            .WithOne()
            .HasForeignKey(x => x.ClaimId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent!.SetField("_alerts");

        builder.Metadata
            .FindNavigation(nameof(Claim.History))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata
            .FindNavigation(nameof(Claim.Alerts))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
