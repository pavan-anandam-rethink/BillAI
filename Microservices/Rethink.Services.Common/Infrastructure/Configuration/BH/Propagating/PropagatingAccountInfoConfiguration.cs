using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Propagating;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Propagating
{
    public class PropagatingAccountInfoConfiguration : IEntityTypeConfiguration<PropagatingAccountInfoEntity>
    {
        public void Configure(EntityTypeBuilder<PropagatingAccountInfoEntity> builder)
        {
            builder.ToTable("hcPropagatingAccountInfo").HasKey(x => x.Id);
        }
    }
}
