using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim.History
{
    public sealed class ClaimHistoryEntityConfiguration : IEntityTypeConfiguration<ClaimHistoryEntity>
    {
        [ExcludeFromCodeCoverage]
        public void Configure(EntityTypeBuilder<ClaimHistoryEntity> builder)
        {
            builder.ToTable("ClaimHistory", "dbo").HasKey(x => x.Id);
            builder.HasOne(x => x.Claim).WithMany(x => x.ClaimHistory).HasForeignKey(x => x.ClaimId);
            builder.HasOne(x => x.ClaimVersion).WithOne(x => x.ClaimHistory).HasForeignKey<ClaimHistoryEntity>(x => x.ClaimVersionId);
        }
    }
}