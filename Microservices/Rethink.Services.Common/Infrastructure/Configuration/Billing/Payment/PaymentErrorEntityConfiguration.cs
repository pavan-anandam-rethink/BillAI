using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentErrorEntityConfiguration : IEntityTypeConfiguration<PaymentErrorEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentErrorEntity> builder)
        {
            builder.ToTable("PaymentError")
                   .HasKey(x => x.Id);

            builder.HasOne(d => d.Payment)
                   .WithMany(p => p.PaymentErrors)
                   .HasForeignKey(d => d.PaymentId);
        }
    }
}