using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim;

[ExcludeFromCodeCoverage]
public class UnProcessedApointmentScheduleEntityConfiguration : IEntityTypeConfiguration<UnProcessedApointmentScheduleEntity>
{
    public void Configure(EntityTypeBuilder<UnProcessedApointmentScheduleEntity> builder)
    {
        builder.ToTable("UnProcessedApointmentSchedule");
        builder.HasKey(x => x.Id);

        // Required fields
        builder.Property(x => x.AccountInfoId)
               .IsRequired();
        builder.Property(x => x.AppointmentId)
               .IsRequired();
        builder.Property(x => x.FunderId)
               .IsRequired();
        builder.Property(x => x.ClaimCreationFrequency)
               .IsRequired();
        builder.Property(x => x.SelectedDays)
               .IsRequired();
        builder.Property(x => x.Frequency)
               .IsRequired();
        builder.Property(x => x.ExecutionTime)
               .IsRequired();
        builder.Property(x => x.UtcExecutionDateTime)
               .HasColumnType("datetime2(3)")
               .IsRequired();
        builder.Property(x => x.TimeZone)
               .IsRequired();
        builder.Property(x => x.ProcessingStatus)
               .IsRequired();
        builder.Property(x => x.CreatedBy)
               .IsRequired();
        builder.Property(x => x.Retry)
               .IsRequired();
        builder.Property(x => x.CreatedOn)
               .HasColumnType("datetime2(3)")
               .IsRequired();
    }
}