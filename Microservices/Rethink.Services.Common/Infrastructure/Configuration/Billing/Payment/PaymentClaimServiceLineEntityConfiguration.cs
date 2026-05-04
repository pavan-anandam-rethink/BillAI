using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentClaimServiceLineEntityConfiguration : IEntityTypeConfiguration<PaymentClaimServiceLineEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentClaimServiceLineEntity> builder)
        {
            builder.ToTable("PaymentClaimServiceLine")
                   .HasKey(x => x.Id);

            builder.HasOne(d => d.PaymentClaim)
                   .WithMany(p => p.PaymentClaimServiceLines)
                   .HasForeignKey(d => d.PaymentClaimId)
                   .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}