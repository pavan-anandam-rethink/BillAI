using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentClaimAttachmentTypeEntityConfiguration : IEntityTypeConfiguration<PaymentClaimAttachmentTypeEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentClaimAttachmentTypeEntity> builder)
        {
            builder.ToTable("PaymentClaimAttachmentType")
                   .HasKey(x => x.Id);
        }

    }
}