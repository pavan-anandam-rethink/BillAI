using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Service;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Service
{
    public class ProviderServiceEntityConfiguration : IEntityTypeConfiguration<ProviderServiceEntity>
    {
        public void Configure(EntityTypeBuilder<ProviderServiceEntity> builder)
        {
            builder.ToTable("hcProviderServices");
            builder.HasKey(x => x.Id);
        }
    }
}
