using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public sealed class ClaimValidationErrorEntityConfiguration : IEntityTypeConfiguration<ClaimValidationErrorEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimValidationErrorEntity> builder)
        {
            builder.ToTable("ClaimValidationErrors", "dbo").HasKey(x => x.Id);
            builder.HasOne(x => x.Claim).WithMany(x => x.ClaimValidationErrors).HasForeignKey(x => x.ClaimId);
            builder.HasOne(x => x.ClaimSubmission).WithMany(x => x.ClaimValidationErrors).HasForeignKey(x => x.ClaimSubmissionId);
            builder.HasOne(x => x.ClaimErrorMessage).WithMany(x => x.ClaimValidationErrors).HasForeignKey(x => x.ClaimErrorMessageId);
            builder.HasOne(x => x.EraValidationError).WithOne(x => x.ClaimValidationError).HasForeignKey<ClaimValidationErrorEntity>(x => x.EraValidationErrorId);
        }
    }
}