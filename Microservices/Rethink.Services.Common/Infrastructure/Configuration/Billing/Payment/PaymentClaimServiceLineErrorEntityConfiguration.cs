using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentClaimServiceLineErrorEntityConfiguration : IEntityTypeConfiguration<PaymentClaimServiceLineErrorEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentClaimServiceLineErrorEntity> builder)
        {
            builder.ToTable("PaymentClaimServiceLineError")
                   .HasKey(x => x.Id);

            builder.HasOne(d => d.PaymentClaimServiceLine)
                .WithMany(p => p.PaymentClaimServiceLineErrors)
                .HasForeignKey(d => d.PaymentClaimServiceLineId);
        }
    }
}