using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim.History
{
    [ExcludeFromCodeCoverage]
    public sealed class ClaimHistoryActionEntityConfiguration : IEntityTypeConfiguration<ClaimHistoryActionEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimHistoryActionEntity> builder)
        {
            builder.ToTable("ClaimHistoryActions", "dbo").HasKey(x => x.Id);
        }
    }
}
