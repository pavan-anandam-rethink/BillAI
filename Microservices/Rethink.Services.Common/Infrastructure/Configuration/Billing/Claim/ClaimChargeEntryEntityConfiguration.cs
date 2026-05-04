using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClaimChargeEntryEntityConfiguration : IEntityTypeConfiguration<ClaimChargeEntryEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimChargeEntryEntity> builder)
        {
            builder.ToTable("ClaimChargeEntries").HasKey(x => x.Id);

            builder.HasOne(x => x.Claim).WithMany(x => x.ClaimChargeEntries).HasForeignKey(x => x.ClaimId);
            builder.Ignore(x => x.ChargePayments);

            builder.Property(e => e.Units)
                .HasColumnName("units")
                .HasColumnType("decimal(18, 3)");

            builder.Property(e => e.UnitRate)
                .HasColumnName("unitRate")
                .HasColumnType("decimal(19, 3)");

            builder.Property(e => e.Charges)
                .HasColumnName("charges")
                .HasColumnType("decimal(18, 3)");
        }
    }
}
