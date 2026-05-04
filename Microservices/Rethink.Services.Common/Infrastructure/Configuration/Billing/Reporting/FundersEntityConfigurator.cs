using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Reporting;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Reporting
{
    [ExcludeFromCodeCoverage]
    public class FundersEntityConfigurator : IEntityTypeConfiguration<FundersEntity>
    {
        public void Configure(EntityTypeBuilder<FundersEntity> builder)
        {
            builder.ToTable("Funders", "reporting").HasKey(x => x.Id);
        }
    }
}
