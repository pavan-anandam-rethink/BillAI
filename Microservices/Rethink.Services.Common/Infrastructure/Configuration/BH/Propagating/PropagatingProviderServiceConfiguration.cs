using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Propagating;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Propagating
{
    public class PropagatingProviderServiceConfiguration : IEntityTypeConfiguration<PropagatingProviderServiceEntity>
    {
        public void Configure(EntityTypeBuilder<PropagatingProviderServiceEntity> builder)
        {
            builder.ToTable("hcPropagatingProviderServices").HasKey(x => x.Id);
        }
    }
}
