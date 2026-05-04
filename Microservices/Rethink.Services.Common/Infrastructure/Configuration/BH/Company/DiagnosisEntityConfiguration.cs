using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Company;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Company
{
    public class DiagnosisEntityConfiguration : IEntityTypeConfiguration<DiagnosisEntity>
    {
        public void Configure(EntityTypeBuilder<DiagnosisEntity> builder)
        {
            builder.ToTable("DiagnosisLU").HasKey(x => x.Id);
        }
    }
}