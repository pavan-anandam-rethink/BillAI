using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Propagating;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Propagating
{
    public class PropagatingFunderConfiguration : IEntityTypeConfiguration<PropagatingFunderEntity>
    {
        public void Configure(EntityTypeBuilder<PropagatingFunderEntity> builder)
        {
            builder.ToTable("hcPropagatingFunders").HasKey(x => x.Id);
        }
    }
}
