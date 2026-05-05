using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rethink.Services.Common.Entities.BH.Scheduling;

namespace Rethink.Services.Common.Infrastructure.Configuration.BH.Scheduling
{
    public class AppointmentEntityConfiguration : IEntityTypeConfiguration<AppointmentEntity>
    {
        public void Configure(EntityTypeBuilder<AppointmentEntity> builder)
        {
            builder.ToTable("hcAppointments").HasKey(x => x.Id);

            //builder.HasOne(x => x.VerifiedByMember).WithMany().HasForeignKey(x => x.VerifiedById);
            //builder.HasOne(x => x.PropagatingStaffMember).WithMany().HasForeignKey(x => x.PropagatingStaffMemberId);
            builder.HasOne(x => x.StaffMember).WithMany().HasForeignKey(x => x.StaffId);

            builder.HasOne(x => x.ChildProfileAuthorizationBillingCode).WithMany(x => x.Appointments).HasForeignKey(x => x.ProcedureCodeId);

            //builder.HasOne(x => x.PropagatingChildProfile).WithMany().HasForeignKey(x => x.PropagatingChildProfileId);
            builder.HasOne(x => x.ChildProfile).WithMany().HasForeignKey(x => x.ClientId);
            //builder.HasOne(x => x.AppointmentOccurrenceType).WithMany().HasForeignKey(x => x.OccurrenceTypeId);
            //builder.HasOne(x => x.AppointmentType).WithMany().HasForeignKey(x => x.AppointmentTypeId);
            //builder.HasOne(x => x.AppointmentCancellationType).WithMany().HasForeignKey(x => x.CancellationTypeId);
            //builder.HasOne(x => x.PropagatingFunder).WithMany().HasForeignKey(x => x.PropagatingFunderId);
            //builder.HasOne(x => x.Funder).WithMany().HasForeignKey(x => x.FunderId);
            //builder.HasOne(x => x.ProviderServiceLine).WithMany().HasForeignKey(x => x.ServiceId);
            //builder.HasOne(x => x.PropagatingProviderService).WithMany().HasForeignKey(x => x.PropagatingProviderServiceId);
            builder.HasOne(x => x.ProviderService).WithMany().HasForeignKey(x => x.ProviderServiceId);
            //builder.HasOne(x => x.SessionNoteReviewedByStaff).WithMany().HasForeignKey(x => x.SessionNoteReviewedBy);

            //builder.HasOne(x => x.AppointmentActivityTag).WithMany(x => x.ActivityAppointments).HasForeignKey(x => x.ActivityTagId);
            //builder.HasOne(x => x.AppointmentCancellationTag).WithMany(x => x.CancelationAppointments).HasForeignKey(x => x.CancellationTagId);
            //builder.HasOne(x => x.PropagatingChildProfileAuthorization).WithMany().HasForeignKey(x => x.PropagatingChildProfileAuthorizationId);

            //builder.HasOne(x => x.Address).WithMany().HasForeignKey(x => x.AddressId);
            //builder.HasOne(x => x.StartingAddress).WithMany().HasForeignKey(x => x.StartingAddressId);
            //builder.HasOne(x => x.EndingAddress).WithMany().HasForeignKey(x => x.EndingAddressId);
            builder.HasOne(x => x.PlaceOfService).WithMany().HasForeignKey(x => x.LocationId);
            builder.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.ToLocationId);
            builder.HasOne(x => x.ProviderServiceLine).WithMany().HasForeignKey(x => x.ServiceId);

            //builder.HasOne(x => x.PropagatingAccountInfo).WithMany().HasForeignKey(x => x.PropagatingAccountInfoId);
            //builder.HasOne(x => x.PropagatingChildProfileFunder).WithMany().HasForeignKey(x => x.PropagatingChildProfileFunderId);
            builder.HasOne(x => x.ProviderBillingCode).WithMany().HasForeignKey(x => x.ProviderBillingCodeId);
            builder.HasOne(x => x.ProviderBillingCodeCredential).WithMany(x => x.Appointments).HasForeignKey(x => x.ProviderBillingCodeCredentialId);

            builder.HasOne(x => x.ModifiedByMember).WithMany().HasForeignKey(x => x.ModifiedBy);


            //builder.HasMany(x => x.SeriesAppointments).WithOne().HasForeignKey(x => x.SeriesAppointmentId); 

        }
    }
}