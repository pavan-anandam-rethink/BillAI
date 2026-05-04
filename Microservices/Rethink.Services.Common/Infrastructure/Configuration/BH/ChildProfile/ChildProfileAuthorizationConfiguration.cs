using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.ChildProfile;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.ChildProfile
{
    public class ChildProfileAuthorizationConfiguration : IEntityTypeConfiguration<ChildProfileAuthorizationEntity>
    {
        public void Configure(EntityTypeBuilder<ChildProfileAuthorizationEntity> builder)
        {
            builder.ToTable("hcChildProfileAuthorization").HasKey(x => x.Id);

            builder.HasOne(x => x.ChildProfileDiagnosis).WithMany().HasForeignKey(x => x.ChildProfileDiagnosisId);
            builder.HasOne(x => x.ChildProfile).WithMany().HasForeignKey(x => x.ChildProfileId);
            builder.HasOne(x => x.ChildProfileFunderServiceLineMapping).WithMany().HasForeignKey(x => x.ChildProfileFunderServiceLineMappingId);
            builder.HasOne(x => x.ProviderServiceLine).WithMany(x => x.ChildProfileAuthorizations).HasForeignKey(x => x.ProviderServiceId);
            builder.HasOne(x => x.Funder).WithMany(x => x.ChildProfileAuthorizations).HasForeignKey(x => x.FunderId);
            builder.HasOne(x => x.ServiceFacilityLocation).WithMany().HasForeignKey(x => x.ServiceFacilityLocationId);
            builder.HasOne(x => x.BillingProvider).WithMany().HasForeignKey(x => x.BillingProviderId);
            builder.HasOne(x => x.ChildProfileReferringProvider).WithMany().HasForeignKey(x => x.ChildProfileReferringProviderId);
            builder.HasOne(x => x.RenderingProvider).WithMany().HasForeignKey(x => x.RenderingProviderStaffId);

        }
    }
}
