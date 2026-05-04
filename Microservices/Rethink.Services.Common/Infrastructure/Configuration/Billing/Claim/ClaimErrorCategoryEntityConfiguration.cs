using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    class ClaimErrorCategoryEntityConfiguration : IEntityTypeConfiguration<ClaimErrorCategoryEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimErrorCategoryEntity> builder)
        {
            builder.ToTable("ClaimErrorCategories", "dbo").HasKey(x => x.Id);
        }
    }
}