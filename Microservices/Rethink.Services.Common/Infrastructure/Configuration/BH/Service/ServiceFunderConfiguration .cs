using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Service;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Service
{
    public class ServiceFunderConfiguration : IEntityTypeConfiguration<ServiceFunderEntity>
    {
        public void Configure(EntityTypeBuilder<ServiceFunderEntity> builder)
        {
            builder.ToTable("hcServiceFunder").HasKey(x => x.Id);

            //builder.HasOne(x => x.ProviderServiceLine).WithMany(x => x.ServiceFunders).HasForeignKey(x => x.ProviderServiceId);
            builder.HasOne(x => x.Funder).WithMany(x => x.ServiceFunders).HasForeignKey(x => x.FunderId);
        }
    }
}
