using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class UnAllocatedPaymentEntityConfiguration : IEntityTypeConfiguration<UnAllocatedPaymentEntity>
    {
        public void Configure(EntityTypeBuilder<UnAllocatedPaymentEntity> builder)
        {
            builder.ToTable("UnAllocatedPayments", "dbo").HasKey(x => x.Id);

            builder.HasOne(x => x.Payment)
                   .WithMany(x => x.UnallocatedPayments)
                   .HasForeignKey(x => x.PaymentId);
        }
    }
}
