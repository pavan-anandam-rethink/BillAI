using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public sealed class ClaimErrorMessageEntityConfiguration : IEntityTypeConfiguration<ClaimErrorMessageEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimErrorMessageEntity> builder)
        {
            builder.ToTable("ClaimErrorMessages", "dbo").HasKey(x => x.Id);
            builder.HasOne(x => x.ClaimErrorCategory).WithMany(x => x.ClaimErrorMessages).HasForeignKey(x => x.ClaimErrorCategoryId);
        }
    }
}