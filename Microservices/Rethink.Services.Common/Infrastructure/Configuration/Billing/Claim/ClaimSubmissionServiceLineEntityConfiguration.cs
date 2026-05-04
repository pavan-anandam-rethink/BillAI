using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClaimSubmissionServiceLineEntityConfiguration : IEntityTypeConfiguration<ClaimSubmissionServiceLineEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimSubmissionServiceLineEntity> builder)
        {
            builder.ToTable("ClaimSubmissionServiceLines");
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.ClaimSubmission).WithMany(x => x.ClaimSubmissionServiceLines).HasForeignKey(x => x.ClaimSubmissionId);
            builder.HasOne(x => x.ClaimChargeEntry).WithMany().HasForeignKey(x => x.ClaimChargeEntryId);
        }
    }
}
