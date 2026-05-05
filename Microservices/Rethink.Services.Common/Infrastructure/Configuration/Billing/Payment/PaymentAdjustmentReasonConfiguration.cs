using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentAdjustmentReasonConfiguration : IEntityTypeConfiguration<PaymentAdjustmentReasonEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentAdjustmentReasonEntity> builder)
        {
            builder.ToTable("PaymentAdjustmentReason")
                   .HasKey(x => x.Id);

        }
    }
}