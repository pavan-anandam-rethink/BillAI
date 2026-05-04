using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Billing;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Billing
{
    public class ReasonCodeEntityConfiguration : IEntityTypeConfiguration<ReasonCodeEntity>
    {
        public void Configure(EntityTypeBuilder<ReasonCodeEntity> builder)
        {
            builder.ToTable("hcReasonCode").HasKey(x => x.Id);
        }
    }
}
