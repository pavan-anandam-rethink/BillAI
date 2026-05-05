using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing
{
    [ExcludeFromCodeCoverage]
    public class ChargePaymentEntityConfiguration : IEntityTypeConfiguration<ChargePaymentEntity>
    {
        public void Configure(EntityTypeBuilder<ChargePaymentEntity> builder)
        {
            builder.ToTable("ChargePayment").HasKey(x => x.Id);

            builder.HasOne(x => x.ChargeEntry)
                   .WithMany(x => x.ChargePayments)
                   .HasForeignKey(x => x.ChargeId);

            builder.Ignore(x => x.ReasonCode);
            //builder.HasOne(x => x.ReasonCode)
            //    .WithMany()
            //    .HasForeignKey(x => x.ReasonCodeId);

            builder.HasOne(x => x.PaymentMethod)
                .WithMany()
                .HasForeignKey(x => x.PaymentMethodId);

            builder.Ignore(x => x.CreatedMember);
            //builder.HasOne(x => x.CreatedMember)
            //    .WithMany()
            //    .HasForeignKey(x => x.CreatedBy);
        }
    }
}
