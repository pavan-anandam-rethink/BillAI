using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    [ExcludeFromCodeCoverage]
    public class ClaimAppointmentLinkEntityConfiguration : IEntityTypeConfiguration<ClaimAppointmentLinkEntity>
    {
        public void Configure(EntityTypeBuilder<ClaimAppointmentLinkEntity> builder)
        {
            builder.ToTable("ClaimAppointmentLink").HasKey(x => x.Id);
            builder.HasOne(x => x.Claim).WithMany(x => x.ClaimAppointmentLinks).HasForeignKey(x => x.ClaimId);
            builder.Ignore(x => x.Appointment);
            builder.HasOne(x => x.ClaimAppointmentLinkChargeEntry).WithMany(x => x.ClaimAppointmentLinks).HasForeignKey(x => x.ClaimAppointmentLinkChargeEntryId);
            builder.HasIndex(x => new { x.AccountInfoId, x.AppointmentId });
        }
    }
}