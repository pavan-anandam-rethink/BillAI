using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Billing;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Billing
{
    public class PayerConfiguration : IEntityTypeConfiguration<PayerEntity>
    {
        public void Configure(EntityTypeBuilder<PayerEntity> builder)
        {
            builder.ToTable("hcPayer").HasKey(x => x.Id);
        }
    }
}
