using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentClaimAdjustmentEntityConfiguration : IEntityTypeConfiguration<PaymentClaimAdjustmentEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentClaimAdjustmentEntity> builder)
        {
            builder.ToTable("PaymentClaimAdjustment")
                   .HasKey(x => x.Id);

            builder.HasOne(d => d.PaymentClaim)
                .WithMany(p => p.PaymentClaimAdjustments)
                .HasForeignKey(d => d.PaymentClaimId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(d => d.PaymentAdjustmentReason)
                .WithMany(p => p.PaymentClaimAdjustments)
                .HasForeignKey(d => d.PaymentAdjustmentReasonId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}