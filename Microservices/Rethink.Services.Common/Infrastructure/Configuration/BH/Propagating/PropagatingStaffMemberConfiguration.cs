using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Propagating;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Propagating
{
    public class PropagatingStaffMemberConfiguration : IEntityTypeConfiguration<PropagatingStaffMemberEntity>
    {
        public void Configure(EntityTypeBuilder<PropagatingStaffMemberEntity> builder)
        {
            builder.ToTable("hcPropagatingStaffMember").HasKey(x => x.Id);
            //builder.HasOne(x => x.StaffTitle).WithMany().HasForeignKey(x => x.StaffTitleId);
        }
    }
}
