using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Service;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Service
{
    public class ProviderServiceLineConfiguration : IEntityTypeConfiguration<ProviderServiceLineEntity>
    {
        public void Configure(EntityTypeBuilder<ProviderServiceLineEntity> builder)
        {
            builder.ToTable("hcProviderServiceLine").HasKey(x => x.Id);

            builder.HasOne(x => x.AccountInfo).WithMany(x => x.ProviderServiceLines).HasForeignKey(x => x.AccountInfoId);
        }
    }
}
