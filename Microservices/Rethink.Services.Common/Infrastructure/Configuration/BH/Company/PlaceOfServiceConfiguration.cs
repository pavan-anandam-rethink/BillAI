using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Company;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Company
{
    public class PlaceOfServiceConfiguration : IEntityTypeConfiguration<PlaceOfServiceEntity>
    {
        public void Configure(EntityTypeBuilder<PlaceOfServiceEntity> builder)
        {
            builder.ToTable("hcPlaceOfService", schema: "dbo");
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.AccountInfo).WithMany().HasForeignKey(x => x.AccountInfoId);
        }
    }
}
