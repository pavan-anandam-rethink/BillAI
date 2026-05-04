using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.ChildProfile;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.ChildProfile
{
    public class ChildProfileDiagnosisEntityConfiguration : IEntityTypeConfiguration<ChildProfileDiagnosisEntity>
    {
        public void Configure(EntityTypeBuilder<ChildProfileDiagnosisEntity> builder)
        {
            builder.ToTable("hcChildProfileDiagnosis");
            builder.HasKey(x => x.Id);
        }
    }
}
