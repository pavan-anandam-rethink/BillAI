using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Billing;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Billing
{
    public class FunderEntityConfiguration : IEntityTypeConfiguration<FunderEntity>
    {
        public void Configure(EntityTypeBuilder<FunderEntity> builder)
        {
            builder.ToTable("hcFunder").HasKey(x => x.Id);

            //builder.HasOne(x => x.AccountInfo).WithMany(x => x.Funders).HasForeignKey(x => x.AccountInfoId);
            //builder.HasOne(x => x.KareoInsuranceCompaniy).WithMany(x => x.Funders).HasForeignKey(x => x.KareoInsuranceCompanyId);
            //builder.HasOne(x => x.FunderType).WithMany().HasForeignKey(x => x.FunderTypeId);
            builder.HasOne(x => x.ProviderLocation).WithMany().HasForeignKey(x => x.ProviderLocationId);
            builder.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId);
        }
    }
}
