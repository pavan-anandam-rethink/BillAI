using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Member
{
    public class AccountRoleConfiguration : IEntityTypeConfiguration<AccountRoleEntity>
    {
        public void Configure(EntityTypeBuilder<AccountRoleEntity> builder)
        {
            builder.ToTable("AccountRole").HasKey(x => x.Id);
        }
    }
}