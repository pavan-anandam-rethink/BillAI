using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Reporting
{
    [ExcludeFromCodeCoverage]
    public class FeatureEntityConfiguration : IEntityTypeConfiguration<FeatureEntity>
    {
        public void Configure(EntityTypeBuilder<FeatureEntity> builder)
        {
            builder.ToTable("Features", "dbo").HasKey(x => x.Id);

            builder.HasMany(x => x.AccountFeaturesSettingsEntity)
               .WithOne(x => x.FeatureEntity)
               .HasForeignKey(x => x.FeatureId)
               .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
