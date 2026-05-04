using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Reporting;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Reporting
{
    [ExcludeFromCodeCoverage]
    public class AccountsReceivableEntityConfiguration : IEntityTypeConfiguration<AccountsReceivableEntity>
    {
        public void Configure(EntityTypeBuilder<AccountsReceivableEntity> builder)
        {
            builder.ToTable("AccountsReceivable", "reporting").HasKey(x => x.Id);
        }
    }
}
