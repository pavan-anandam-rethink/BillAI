using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentTypesConfiguration : IEntityTypeConfiguration<PaymentTypeEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentTypeEntity> builder)
        {
            builder.ToTable("PaymentType")
                .HasKey(x => x.Id);
        }
    }
}
