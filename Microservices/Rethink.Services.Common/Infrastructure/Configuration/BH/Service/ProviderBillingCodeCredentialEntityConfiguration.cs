using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Service;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Service
{
    public class ProviderBillingCodeCredentialEntityConfiguration : IEntityTypeConfiguration<ProviderBillingCodeCredentialEntity>
    {
        public void Configure(EntityTypeBuilder<ProviderBillingCodeCredentialEntity> builder)
        {
            builder.ToTable("hcProviderBillingCodeCredentials").HasKey(x => x.Id);

            builder.HasOne(x => x.ProviderBillingCode).WithMany(x => x.ProviderBillingCodeCredentials).HasForeignKey(x => x.ProviderBillingCodeId);
            //builder.HasOne(x => x.StaffCredential).WithMany().HasForeignKey(x => x.StaffCredentialId);
        }
    }
}
