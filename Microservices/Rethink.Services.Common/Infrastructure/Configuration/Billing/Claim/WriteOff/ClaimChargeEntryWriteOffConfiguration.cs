using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim.WriteOff
{
    [ExcludeFromCodeCoverage]
    public class ClaimChargeEntryWriteOffConfiguration : IEntityTypeConfiguration<ClaimChargeEntryWriteOffEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimChargeEntryWriteOffEntity> builder)
        {
            builder.ToTable("ClaimChargeEntryWriteOff")
                   .HasKey(x => x.Id);

            builder.HasOne(d => d.ClaimWriteOff)
                .WithMany(p => p.ClaimChargeEntryWriteOffs)
                .HasForeignKey(d => d.ClaimWriteOffId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(d => d.WriteOffReasonCode)
                .WithMany(p => p.ClaimChargeEntryWriteOffs)
                .HasForeignKey(d => d.WriteOffReasonCodeId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(d => d.ClaimChargeEntry)
                .WithMany(p => p.ClaimChargeEntryWriteOffs)
                .HasForeignKey(d => d.ClaimChargeEntryId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
