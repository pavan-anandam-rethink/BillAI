using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class
        PaymentClaimServiceLineAdjustmentEntityConfiguration : IEntityTypeConfiguration<
            PaymentClaimServiceLineAdjustmentEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentClaimServiceLineAdjustmentEntity> builder)
        {
            builder.ToTable("PaymentClaimServiceLineAdjustment")
                   .HasKey(x => x.Id);

            builder.HasOne(d => d.PaymentClaimServiceLine)
                   .WithMany(p => p.PaymentClaimServiceLineAdjustments)
                   .HasForeignKey(d => d.PaymentClaimServiceLineId)
                   .OnDelete(DeleteBehavior.ClientSetNull);

            //builder.HasOne(d => d.PaymentAdjustmentReason)
            //       .WithMany(p => p.PaymentClaimServiceLineAdjustments)
            //       .HasForeignKey(d => d.PaymentAdjustmentReasonId)
            //       .OnDelete(DeleteBehavior.ClientSetNull);
        }

    }
}