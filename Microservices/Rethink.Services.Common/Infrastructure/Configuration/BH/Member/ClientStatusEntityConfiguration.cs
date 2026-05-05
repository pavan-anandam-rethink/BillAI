using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.ChildProfile;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Member
{
    public class ClientStatusEntityConfiguration : IEntityTypeConfiguration<ClientStatusEntity>
    {
        public void Configure(EntityTypeBuilder<ClientStatusEntity> builder)
        {
            builder.ToTable("hcClientStatus").HasKey(x => x.Id);

            builder.HasOne(x => x.AccountInfo).WithMany(x => x.ClientStatuses).HasForeignKey(x => x.AccountInfoId);
        }
    }
}
