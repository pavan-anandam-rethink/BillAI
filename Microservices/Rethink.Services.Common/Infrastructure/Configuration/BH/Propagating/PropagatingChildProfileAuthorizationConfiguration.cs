using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Propagating;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Propagating
{
    public class PropagatingChildProfileAuthorizationConfiguration : IEntityTypeConfiguration<PropagatingChildProfileAuthorizationEntity>
    {
        public void Configure(EntityTypeBuilder<PropagatingChildProfileAuthorizationEntity> builder)
        {
            builder.ToTable("hcPropagatingChildProfileAuthorization").HasKey(x => x.Id);
            builder.HasOne(x => x.ModifiedByMember).WithMany(x=>x.PropagatingChildProfileAuthorizationEntities).HasForeignKey(x => x.CreatedBy);
        }
    }
}
