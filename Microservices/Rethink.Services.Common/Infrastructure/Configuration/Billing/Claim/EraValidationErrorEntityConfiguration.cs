using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim.Validation;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public sealed class EraValidationErrorEntityConfiguration : IEntityTypeConfiguration<EraValidationErrorEntity>
    {
        public void Configure(EntityTypeBuilder<EraValidationErrorEntity> builder)
        {
            builder.ToTable("EraValidationErrors", "dbo").HasKey(x => x.Id);
            builder.HasOne(x => x.GroupCode).WithMany().HasForeignKey(x => x.GroupCodeId);
            builder.HasOne(x => x.AdjustmentCode).WithMany().HasForeignKey(x => x.AdjustmentCodeId);
            builder.Property(x => x.EntityIdentifierCode).HasMaxLength(10).IsRequired(false);
            builder.Property(x => x.StcPosition).HasMaxLength(10).IsRequired(false);
        }
    }
}
