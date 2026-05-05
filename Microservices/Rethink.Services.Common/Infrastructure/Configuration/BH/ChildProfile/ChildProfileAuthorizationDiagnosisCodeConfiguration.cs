using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.ChildProfile;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.ChildProfile
{
    public class ChildProfileAuthorizationDiagnosisCodeConfiguration
        : IEntityTypeConfiguration<ChildProfileAuthorizationDiagnosisCodeEntity>
    {
        public void Configure(EntityTypeBuilder<ChildProfileAuthorizationDiagnosisCodeEntity> builder)
        {
            builder.ToTable("hcChildProfileAuthorizationDiagnosisCode").HasKey(x => x.Id);

            builder.HasOne(x => x.ChildProfileDiagnosis).WithMany().HasForeignKey(x => x.ChildProfileDiagnosisId);
            builder.HasOne(x => x.ChildProfileAuthorization)
                .WithMany(x => x.ChildProfileAuthorizationDiagnosisCodes)
                .HasForeignKey(x => x.ChildProfileAuthorizationId);
            builder.HasOne(x => x.Diagnosis).WithMany().HasForeignKey(x => x.DiagnosisId);
        }
    }
}