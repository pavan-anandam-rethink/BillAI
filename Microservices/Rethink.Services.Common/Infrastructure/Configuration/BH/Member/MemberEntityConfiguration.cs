using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Member
{
    public class MemberEntityConfiguration : IEntityTypeConfiguration<MemberEntity>
    {
        public void Configure(EntityTypeBuilder<MemberEntity> builder)
        {
            builder.ToTable("tblMember").HasKey(x => x.Id);

            builder.HasOne(x => x.AccountInfo).WithMany(x => x.Members).HasForeignKey(x => x.AccountInfoId);

            //builder.HasMany(x => x.MemberAccountRoles).WithOne(x => x.Member).HasForeignKey(x => x.AccountRoleId);
            //builder.HasMany(x => x.MemberAccountRoles).WithOne(x => x.Member).HasForeignKey(x => x.AccountRoleId);

        }
    }
}
