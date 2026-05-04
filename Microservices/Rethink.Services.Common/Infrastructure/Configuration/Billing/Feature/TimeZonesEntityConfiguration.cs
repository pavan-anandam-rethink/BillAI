using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Feature;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Reporting
{
    [ExcludeFromCodeCoverage]
    public class TimeZonesEntityConfiguration : IEntityTypeConfiguration<TimezonesEntity>
    {
        public void Configure(EntityTypeBuilder<TimezonesEntity> builder)
        {
            builder.ToTable("hcTimezones", "dbo").HasKey(x => x.Id);
        }
    }
}
