using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim.Validation;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Era
{
    [ExcludeFromCodeCoverage]
    public sealed class ExternalCodeConfiguration : IEntityTypeConfiguration<ExternalCodeEntity>
    {
        public void Configure(EntityTypeBuilder<ExternalCodeEntity> builder)
        {
            builder.ToTable("ExternalCodes", "dbo").HasKey(x => x.Id);
        }
    }
}
