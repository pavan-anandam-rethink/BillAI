using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Reporting
{
    [ExcludeFromCodeCoverage]
    public class ClaimTransactionEntityConfiguration : IEntityTypeConfiguration<ClaimTransactionEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimTransactionEntity> builder)
        {
            builder.ToTable("ClaimTransactions").HasKey(x => x.Id);
        }
    }
}
