using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim.WriteOff
{
    [ExcludeFromCodeCoverage]
    public class ClaimWriteOffConfiguration : IEntityTypeConfiguration<ClaimWriteOffEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimWriteOffEntity> builder)
        {
            builder.ToTable("ClaimWriteOff")
                   .HasKey(x => x.Id);

            builder.HasOne(d => d.Claim)
                .WithMany(p => p.ClaimWriteOffs)
                .HasForeignKey(d => d.ClaimId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(d => d.WriteOffAction)
                .WithMany(p => p.ClaimWriteOffs)
                .HasForeignKey(d => d.WriteOffActionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(d => d.WriteOffApplication)
                .WithMany(p => p.ClaimWriteOffs)
                .HasForeignKey(d => d.WriteOffApplicationId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
