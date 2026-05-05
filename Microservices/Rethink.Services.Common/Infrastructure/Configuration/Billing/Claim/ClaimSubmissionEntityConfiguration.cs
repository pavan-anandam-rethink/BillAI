using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClaimSubmissionEntityConfiguration : IEntityTypeConfiguration<ClaimSubmissionEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimSubmissionEntity> builder)
        {
            builder.ToTable("ClaimSubmissions");
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Claim).WithMany(c => c.ClaimSubmissions).HasForeignKey(x => x.ClaimId);
            builder.HasOne(x => x.PriorClaimSubmission).WithOne(x => x.NextClaimSubmission).HasForeignKey<ClaimSubmissionEntity>(x => x.PriorClaimSubmissionId);
            builder.Ignore(x => x.ChildProfileAuthorization);
            builder.HasMany(x => x.ClaimSubmissionServiceLines).WithOne(x => x.ClaimSubmission).HasForeignKey(x => x.ClaimSubmissionId);

        }
    }
}
