using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Reporting;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Reporting
{
    [ExcludeFromCodeCoverage]
    public class ClaimStatusEntityConfigurator : IEntityTypeConfiguration<ClaimStatusEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimStatusEntity> builder)
        {
            builder.ToTable("ClaimStatus", "reporting").HasKey(e => e.Id);
        }
    }
}
