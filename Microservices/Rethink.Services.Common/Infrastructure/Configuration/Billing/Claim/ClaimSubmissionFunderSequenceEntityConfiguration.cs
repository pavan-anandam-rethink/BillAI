using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClaimSubmissionFunderSequenceEntityConfiguration : IEntityTypeConfiguration<ClaimSubmissionFunderSequenceEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimSubmissionFunderSequenceEntity> builder)
        {
            builder.ToTable("ClaimSubmissionFunderSequence");
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.ClaimSubmission).WithMany(x => x.ClaimSubmissionFunderSequences).HasForeignKey(x => x.ClaimSubmissionId);
        }
    }
}
