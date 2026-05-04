using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Service;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Service
{
    public class ProviderBillingCodeEntityConfiguration : IEntityTypeConfiguration<ProviderBillingCodeEntity>
    {
        public void Configure(EntityTypeBuilder<ProviderBillingCodeEntity> builder)
        {
            builder.ToTable("hcProviderBillingCode").HasKey(x => x.Id);

            //builder.HasOne(x => x.Funder).WithMany().HasForeignKey(x => x.FunderId);
            builder.HasOne(x => x.UnitType).WithMany().HasForeignKey(x => x.UnitTypeId);
            builder.HasOne(x => x.UnitType2).WithMany().HasForeignKey(x => x.UnitTypeId2);
            builder.HasOne(x => x.ProviderService).WithMany().HasForeignKey(x => x.ServiceId);
            builder.HasOne(x => x.AccountInfo).WithMany().HasForeignKey(x => x.AccountInfoId);
            builder.HasOne(x => x.RenderingProviderStaff).WithMany().HasForeignKey(x => x.RenderingProviderStaffId);
        }
    }
}
