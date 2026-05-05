using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Propagating;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Propagating
{
    public class PropagatingChildProfileFunderConfiguration : IEntityTypeConfiguration<PropagatingChildProfileFunderEntity>
    {
        public void Configure(EntityTypeBuilder<PropagatingChildProfileFunderEntity> builder)
        {
            builder.ToTable("hcPropagatingChildProfileFunder").HasKey(x => x.Id);
        }
    }
}
