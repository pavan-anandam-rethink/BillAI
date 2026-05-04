using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    public class FunderSettingsEntityConfiguration : IEntityTypeConfiguration<FunderSettingsEntity>
    {
        public void Configure(EntityTypeBuilder<FunderSettingsEntity> builder)
        {
            builder.ToTable("FunderSettings");

            builder.HasKey(x => x.Id);


            builder.Property(x => x.AccountInfoId)
                .IsRequired();

            builder.Property(x => x.FunderId)
                .IsRequired();

            builder.Property(x => x.FunderName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.ClaimFilingIndicatorId)
                .IsRequired();

            builder.Property(x => x.IncludeTaxonomyCode)
                .IsRequired();

            builder.Property(x => x.DateCreated)
                .IsRequired();

            builder.Property(x => x.DateLastModified)
                .IsRequired(false);

            builder.Property(x => x.DateDeleted)
                .IsRequired(false);

            builder.Property(x => x.CreatedBy)
                .IsRequired();

            builder.Property(x => x.ModifiedBy)
                .IsRequired(false);

            builder.Property(x => x.DeletedBy)
                .IsRequired(false);

            builder.HasOne(x => x.ClaimFilingIndicator)
                .WithMany()
                .HasForeignKey(x => x.ClaimFilingIndicatorId);

            builder.Property(x => x.ScheduleType)
                 .IsRequired(false);
            builder.Property(x => x.ScheduleTime)
                 .IsRequired(false);
            builder.Property(x => x.ScheduleTimeZone)
                 .IsRequired(false);
            builder.Property(x => x.WeeklyDays)
                 .IsRequired(false);
            builder.Property(x => x.MonthlyFrequency)
                 .IsRequired(false);
            builder.Property(x => x.CombineChargesForSameClient)
                 .IsRequired(false);
        }
    }
}