using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClaimDiagnosisCodeEntityConfiguration : IEntityTypeConfiguration<ClaimDiagnosisCodeEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimDiagnosisCodeEntity> builder)
        {
            builder.ToTable("ClaimDiagnosisCode").HasKey(x => x.Id);

            builder.HasOne(x => x.Claim)
                .WithMany(x => x.ClaimDiagnosisCodes)
                .HasForeignKey(x => x.ClaimId);
            builder.Ignore(x => x.Diagnosis);
        }
    }
}