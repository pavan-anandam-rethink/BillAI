using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClearingHouseResponseEntityConfiguration : IEntityTypeConfiguration<ClearingHouseResponseDetailsEntity>
    {
        public void Configure(EntityTypeBuilder<ClearingHouseResponseDetailsEntity> builder)
        {
            builder.ToTable("ClearingHouseResponseDetails", "dbo").HasKey(x => x.Id);
        }
    }
}
