using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Propagating;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Propagating
{
    public class PropagatingChildProfileConfiguration : IEntityTypeConfiguration<PropagatingChildProfileEntity>
    {
        public void Configure(EntityTypeBuilder<PropagatingChildProfileEntity> builder)
        {
            builder.ToTable("hcPropagatingChildProfile").HasKey(x => x.Id);
        }
    }
}
