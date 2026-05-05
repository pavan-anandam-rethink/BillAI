using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Infrastructure.Configuration.Billing.Claim
{
    public class AppointmentClaimProcessingErrorConfiguration : IEntityTypeConfiguration<AppointmentClaimProcessingErrorEntity>
    {
        public void Configure(EntityTypeBuilder<AppointmentClaimProcessingErrorEntity> builder)
        {
            builder.ToTable("AppointmentClaimProcessingError").HasKey(x => x.Id);

            //builder.HasOne(x => x.ClaimAppointmentLink).WithMany().HasForeignKey(x => x.AppointmentId);
            builder.HasOne(x => x.ClaimAppointmentLink)  // Define the navigation property
            .WithMany()  // Indicating one-to-many relationship (assuming one ClaimAppointmentLink can be related to multiple AppointmentClaimProcessingError entities)
            .HasForeignKey(x => x.ClaimAppointmentLinkId)  // Foreign key property in AppointmentClaimProcessingError
            .OnDelete(DeleteBehavior.Restrict);  // Define the delete behavior (can be Cascade or SetNull as well)

        }
    }
}
