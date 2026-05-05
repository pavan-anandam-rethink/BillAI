using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Company;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Company
{
    public class LocationCodeEntityConfiguration : IEntityTypeConfiguration<LocationCodeEntity>
    {
        public void Configure(EntityTypeBuilder<LocationCodeEntity> builder)
        {
            builder.ToTable("hcLocationCodes").HasKey(x => x.Id);
        }
    }
}
