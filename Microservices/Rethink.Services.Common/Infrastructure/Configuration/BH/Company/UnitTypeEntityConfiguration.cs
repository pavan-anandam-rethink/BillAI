using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Company;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Company
{
    public class UnitTypeEntityConfiguration : IEntityTypeConfiguration<UnitTypeEntity>
    {
        public void Configure(EntityTypeBuilder<UnitTypeEntity> builder)
        {
            builder.ToTable("hcUnitType").HasKey(x => x.Id);
        }
    }
}
