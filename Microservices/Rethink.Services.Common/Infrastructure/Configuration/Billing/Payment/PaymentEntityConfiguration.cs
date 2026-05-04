using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentEntityConfiguration : IEntityTypeConfiguration<PaymentEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentEntity> builder)
        {
            builder.ToTable("Payment")
                   .HasKey(x => x.Id);

            builder.Ignore(x => x.IsErrorPayment);

            builder.HasOne(x => x.PaymentEraUpload)
                   .WithOne(x => x.Payment)
                   .HasForeignKey<PaymentEntity>(x => x.PaymentEraUploadId);

            builder.HasOne(x => x.PaymentMethodEntity)
                   .WithMany(x => x.Payments)
                   .HasForeignKey(x => x.PaymentMethodId);

            builder.HasOne(x => x.PaymentTypeEntity)
                .WithMany().HasForeignKey(x => x.PaymentTypeId);
        }
    }
}