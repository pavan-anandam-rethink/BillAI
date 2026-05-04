using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentClaimAttachmentEntityConfiguration : IEntityTypeConfiguration<PaymentClaimAttachmentEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentClaimAttachmentEntity> builder)
        {
            builder.ToTable("PaymentClaimAttachment")
                   .HasKey(x => x.Id);

            builder.HasOne(d => d.PaymentClaimAttachmentType)
                   .WithMany(p => p.PaymentClaimAttachments)
                   .HasForeignKey(d => d.PaymentClaimAttachmentTypeId)
                   .OnDelete(DeleteBehavior.ClientSetNull);
        }

    }
}