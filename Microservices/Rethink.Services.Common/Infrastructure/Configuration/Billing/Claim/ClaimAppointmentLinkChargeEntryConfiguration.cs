using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClaimAppointmentLinkChargeEntryConfiguration : IEntityTypeConfiguration<ClaimAppointmentLinkChargeEntry>
    {
        public void Configure(EntityTypeBuilder<ClaimAppointmentLinkChargeEntry> builder)
        {
            builder.ToTable("ClaimAppointmentLinkChargeEntry").HasKey(x => x.Id);

            builder.HasOne(x => x.ClaimChargeEntry).WithMany().HasForeignKey(x => x.ClaimChargeEntryEntityId);
        }
    }
}
