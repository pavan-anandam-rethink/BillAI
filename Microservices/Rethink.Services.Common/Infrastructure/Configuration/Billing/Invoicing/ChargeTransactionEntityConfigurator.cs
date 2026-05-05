using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Reporting
{
    [ExcludeFromCodeCoverage]
    public class ChargeTransactionEntityConfigurator : IEntityTypeConfiguration<ChargeTransactionEntity>
    {
        public void Configure(EntityTypeBuilder<ChargeTransactionEntity> builder)
        {
            builder.ToTable("ChargeTransactions").HasKey(x => x.Id);
        }
    }
}
