using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Member
{
    public class StaffTitleConfiguration : IEntityTypeConfiguration<StaffTitleEntity>
    {
        public void Configure(EntityTypeBuilder<StaffTitleEntity> builder)
        {
            builder.ToTable("hcStaffTitle").HasKey(x => x.Id);

            builder.HasOne(x => x.AccountRole).WithMany().HasForeignKey(x => x.RoleTypeId);
        }
    }
}