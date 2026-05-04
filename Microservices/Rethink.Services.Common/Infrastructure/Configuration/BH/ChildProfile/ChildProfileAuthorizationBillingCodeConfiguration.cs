using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.ChildProfile;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.ChildProfile
{
    public class ChildProfileAuthorizationBillingCodeConfiguration : IEntityTypeConfiguration<ChildProfileAuthorizationBillingCodeEntity>
    {
        public void Configure(EntityTypeBuilder<ChildProfileAuthorizationBillingCodeEntity> builder)
        {
            builder.ToTable("hcChildProfileAuthorizationBillingCode").HasKey(x => x.Id);

            builder.HasOne(x => x.ChildProfileAuthorization).WithMany(x => x.ChildProfileAuthorizationBillingCodes).HasForeignKey(x => x.ChildProfileAuthorizationId);
            //builder.HasOne(x => x.ProviderBillingCode).WithMany(x => x.ChildProfileAuthorizationBillingCodes).HasForeignKey(x => x.ProviderBillingCodeId);
            builder.HasOne(x => x.UnitType).WithMany().HasForeignKey(x => x.UnitTypeId);
            //builder.HasOne(x => x.ProviderService).WithMany(x => x.ChildProfileAuthorizationBillingCodes).HasForeignKey(x => x.ProviderServiceId);
        }
    }
}
