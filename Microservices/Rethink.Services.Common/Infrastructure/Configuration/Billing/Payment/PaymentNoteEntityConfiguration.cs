using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentNoteEntityConfiguration : IEntityTypeConfiguration<PaymentNoteEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentNoteEntity> builder)
        {
            builder.ToTable("PaymentNotes")
                   .HasKey(x => x.Id);

            builder.HasOne(d => d.Paymant)
                   .WithMany(x => x.Notes)
                   .HasForeignKey(d => d.PaymentId);
        }
    }
}