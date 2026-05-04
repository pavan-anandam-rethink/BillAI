using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentAttachmentEntityConfiguration : IEntityTypeConfiguration<PaymentAttachmentEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentAttachmentEntity> builder)
        {
            builder.ToTable("PaymentAttachment")
                   .HasKey(x => x.Id);

            //builder.HasOne(x => x.Member)
            //       .WithMany()
            //       .HasForeignKey(x => x.CreatedBy);
        }

    }
}