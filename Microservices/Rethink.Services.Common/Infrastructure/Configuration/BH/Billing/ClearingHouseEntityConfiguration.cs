using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Billing;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Billing
{
    public class ClearingHouseEntityConfiguration : IEntityTypeConfiguration<ClearingHouseEntity>
    {
        public void Configure(EntityTypeBuilder<ClearingHouseEntity> builder)
        {
            builder.ToTable("ClearingHouse");
            builder.HasKey(x => x.Id);
        }
    }
}
