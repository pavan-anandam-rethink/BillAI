using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Member
{
    public class StaffMemberConfiguraion : IEntityTypeConfiguration<StaffMemberEntity>
    {
        public void Configure(EntityTypeBuilder<StaffMemberEntity> builder)
        {
            builder.ToTable("hcStaffMember").HasKey(x => x.Id);

            builder.HasOne(x => x.StaffStatus).WithMany().HasForeignKey(x => x.StaffStatusId);
            builder.HasOne(x => x.StaffTitle).WithMany().HasForeignKey(x => x.TitleTypeId);
            //builder.HasOne(x => x.Member).WithMany(x => x.StaffMembers).HasForeignKey(x => x.MemberId);
            builder.HasOne(x => x.TimeZone).WithMany().HasForeignKey(x => x.TimezoneId);
            builder.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId);
            //builder.HasOne(x => x.StaffCertification).WithMany().HasForeignKey(x => x.StaffCertificationId);
            //builder.HasOne(x => x.Supervisor).WithMany().HasForeignKey(x => x.SupervisorId);
        }
    }
}