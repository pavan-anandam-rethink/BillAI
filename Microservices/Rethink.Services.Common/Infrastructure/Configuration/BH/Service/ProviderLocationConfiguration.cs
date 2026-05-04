using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Service;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Service
{
    public class ProviderLocationConfiguration : IEntityTypeConfiguration<ProviderLocationEntity>
    {
        public void Configure(EntityTypeBuilder<ProviderLocationEntity> builder)
        {

            builder.ToTable("hcProviderLocation").HasKey(x => x.Id);

            builder.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId);
        }
    }
}
