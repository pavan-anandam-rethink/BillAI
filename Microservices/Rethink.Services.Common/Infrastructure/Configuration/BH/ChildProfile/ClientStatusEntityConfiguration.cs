using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.ChildProfile;

namespace RethinkAutism.Data.Configuration.CompanyAccount
{
    class ClientStatusEntityConfiguration : IEntityTypeConfiguration<ClientStatusEntity>
    {
        public void Configure(EntityTypeBuilder<ClientStatusEntity> builder)
        {
            builder.ToTable("hcClientStatus", schema: "dbo").HasKey(x => x.Id);

            builder.HasOne(x => x.AccountInfo).WithMany(x => x.ClientStatuses).HasForeignKey(x => x.AccountInfoId);
        }
    }
}