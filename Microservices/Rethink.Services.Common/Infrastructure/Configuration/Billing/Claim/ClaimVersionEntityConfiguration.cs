using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClaimVersionEntityConfiguration : IEntityTypeConfiguration<ClaimVersionEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimVersionEntity> builder)
        {
            builder.ToTable("ClaimVersions", "dbo").HasKey(x => x.Id);
        }
    }
}
