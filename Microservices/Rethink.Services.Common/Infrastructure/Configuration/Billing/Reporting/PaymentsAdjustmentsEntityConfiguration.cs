using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Reporting;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Reporting
{
    [ExcludeFromCodeCoverage]
    public class PaymentsAdjustmentsEntityConfiguration : IEntityTypeConfiguration<PaymentsAdjustmentsEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentsAdjustmentsEntity> builder)
        {
            builder.ToTable("PaymentsAdjustments", "reporting").HasKey(x => x.Id);
        }
    }
}
