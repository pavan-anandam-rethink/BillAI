using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentClaimErrorEntityConfiguration : IEntityTypeConfiguration<PaymentClaimErrorEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentClaimErrorEntity> builder)
        {
            builder.ToTable("PaymentClaimError")
                   .HasKey(x => x.Id);

            builder.HasOne(d => d.PaymentClaim)
                   .WithMany(p => p.PaymentClaimErrors)
                   .HasForeignKey(d => d.PaymentClaimId);
        }
    }
}