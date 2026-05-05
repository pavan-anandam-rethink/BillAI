using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Reporting
{
    [ExcludeFromCodeCoverage]
    public class AccountFeatureSettingEntityConfiguration : IEntityTypeConfiguration<AccountFeatureSettingEntity>
    {
        public void Configure(EntityTypeBuilder<AccountFeatureSettingEntity> builder)
        {

            builder.ToTable("AccountFeatureSettings", "dbo");

            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.FeatureEntity)                 // 🔥 Use navigation property here
               .WithMany(f => f.AccountFeaturesSettingsEntity)  // 🔥 Use navigation property on FeatureEntity
               .HasForeignKey(x => x.FeatureId)
               .HasConstraintName("FK_AccountFeatureSettings_Features")
               .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
