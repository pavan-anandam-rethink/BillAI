using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentEraUploadEntityConfiguration : IEntityTypeConfiguration<PaymentEraUploadEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentEraUploadEntity> builder)
        {
            builder.ToTable("PaymentEraUpload")
                   .HasKey(x => x.Id);

        }
    }
}