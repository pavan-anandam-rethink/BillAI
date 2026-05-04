using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Company;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Company
{
    public class TimeZoneEntityConfiguration : IEntityTypeConfiguration<TimeZoneEntity>
    {
        public void Configure(EntityTypeBuilder<TimeZoneEntity> builder)
        {
            builder.ToTable("hcTimezones");
            builder.HasKey(x => x.Id);
        }
    }
}