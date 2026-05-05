using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Billing;
using Rethink.Services.Common.Entities.BH.ChildProfile;
using Rethink.Services.Common.Entities.BH.Company;
using Rethink.Services.Common.Entities.BH.Curriculum;
using Rethink.Services.Common.Entities.BH.Member;
using Rethink.Services.Common.Entities.BH.Propagating;
using Rethink.Services.Common.Entities.BH.Service;
using Rethink.Services.Common.Entities.Billing.Scheduling;

namespace Rethink.Services.Common.Entities.BH.Scheduling
{
    public class AppointmentEntity : BasePersistEntity, IAuditedEntity
    {
        [Key]
        [Column("AppointmentId")]
        public override int Id { get; set; }
        public int AppointmentTypeId { get; set; }
        public string AppointmentDescription { get; set; }

        public int StaffId { get; set; }
        public int? ClientId { get; set; }
        public int OccurrenceTypeId { get; set; }
        public int OccurrenceFrequency { get; set; }
        public int FrequencyInterval { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public int DayTypes { get; set; }
        public int MonthDay { get; set; }
        // public int ScheduledById { get; set; }
        public DateTime? ClientVerificationDate { get; set; }
        public DateTime? StaffVerificationDate { get; set; }
        public int? CancellationTypeId { get; set; }
        public string CancellationNote { get; set; }
        public DateTime? RescheduleDueDate { get; set; }
        public int? RescheduleAssignedToId { get; set; }
        public int? SessionNoteResponseId { get; set; }
        public int? SessionNoteFormId { get; set; }
        public int? ServiceId { get; set; }
        public int? PaycodeId { get; set; }
        public int? FunderId { get; set; }
        public int? ProcedureCodeId { get; set; }
        public int? LocationId { get; set; }
        public int? AddressId { get; set; }
        public Guid? ClientSignatureId { get; set; }
        public Guid? StaffSignatureId { get; set; }
        public int? AssociatedAppointmentId { get; set; }
        public int? DiagnosisId { get; set; }
        public DateTime? SessionNoteReviewedOn { get; set; }
        public int? StartingAddressId { get; set; }
        public int? EndingAddressId { get; set; }
        public int? SessionNoteReviewedBy { get; set; }
        public int? SeriesAppointmentId { get; set; }
        public DateTime? OccurrenceEndDate { get; set; }
        public int? ActualStartTime { get; set; }
        public int? ActualEndTime { get; set; }
        public string SignatureParentName { get; set; }
        public string SignatureParentRelationship { get; set; }
        public int? FromLocationId { get; set; }
        public string FromLocation { get; set; }
        public int? ToLocationId { get; set; }
        public string ToLocation { get; set; }
        public decimal? Mileage { get; set; }
        public int? ActivityTagId { get; set; }
        public int? CancellationTagId { get; set; }
        public int? CopaymentMethodId { get; set; }
        public string CopaymentReferenceNumber { get; set; }
        public decimal? CopaymentAmountCollected { get; set; }
        public int? ProviderServiceId { get; set; }
        public int? RescheduleFromId { get; set; }

        [Column("hcProviderBillingCodeCredentialId")]
        public int? ProviderBillingCodeCredentialId { get; set; }
        [Column("hcPropagatingAccountInfoId")]
        public int? PropagatingAccountInfoId { get; set; }
        [Column("hcPropagatingProviderServiceId")]
        public int? PropagatingProviderServiceId { get; set; }
        [Column("hcPropagatingStaffMemberId")]
        public int? PropagatingStaffMemberId { get; set; }
        [Column("hcPropagatingChildProfileId")]
        public int? PropagatingChildProfileId { get; set; }
        [Column("hcPropagatingChildProfileAuthorizationId")]
        public int? PropagatingChildProfileAuthorizationId { get; set; }
        [Column("hcPropagatingFunderId")]
        public int? PropagatingFunderId { get; set; }
        [Column("hcPropagatingChildProfileFunderId")]
        public int? PropagatingChildProfileFunderId { get; set; }
        [Column("hcPropagatingProviderBillingCodeId")]
        public int? PropagatingProviderBillingCodeId { get; set; }
        [Column("hcPropagatingClientAuthRenderingProviderId")]
        public int? PropagatingClientAuthRenderingProviderId { get; set; }
        [Column("hcPropagatingClientAuthReferringProviderId")]
        public int? PropagatingClientAuthReferringProviderId { get; set; }
        [Column("hcPropagatingClientAuthBillingProviderId")]
        public int? PropagatingClientAuthBillingProviderId { get; set; }
        [Column("hcPropagatingClientAuthServiceFacilityLocationId")]
        public int? PropagatingClientAuthServiceFacilityLocationId { get; set; }
        public DateTime? SeriesAppointmentStartDate { get; set; }
        public int? MonthTypeId { get; set; }
        public int? MonthOccurrenceDayId { get; set; }
        public int? MonthOccurrenceTypeId { get; set; }
        public DateTime? DateBillingReported { get; set; }
        public int? VerifiedById { get; set; }
        public string Notes { get; set; }
        public int? ProcedureCodeIdPreviousReference { get; set; }
        public int? SessionNoteDraftFormId { get; set; }
        public int? SessionNoteDraftResponseId { get; set; }
        public DateTime? SessionNoteDraftOn { get; set; }
        public int? SessionNoteDraftStaffMemberId { get; set; }
        public int? KareoEncounterId { get; set; }
        public decimal? ParentLatitude { get; set; }
        public decimal? ParentLongitude { get; set; }
        public decimal? StaffMemberLatitude { get; set; }
        public decimal? StaffMemberLongitude { get; set; }
        public string StaffVerifiedAddress { get; set; }
        public string ParentVerifiedAddress { get; set; }
        public DateTime? AdminVerificationDate { get; set; }
        public int? AdminVerifiedBy { get; set; }
        public DateTime? DatePayrollReported { get; set; }
        public bool? VerifiedFromSessionNote { get; set; }
        [Column("hcProviderBillingCodeId")]
        public int? ProviderBillingCodeId { get; set; }
        public DateTime? DateDoeBilled { get; set; }
        public DateTime? PrincipalVerificationDate { get; set; }
        public Guid? PrincipalSignatureId { get; set; }
        public DateTime? InitOccurrenceStartDate { get; set; }

        public int? WorkflowHistoryId { get; set; }
        [Column("hcClientFunderId")]
        public int? ClientFunderId { get; set; }


        public int CreatedBy { get; set; }
        public int ScheduledById { get; set; }
        [Column("ScheduledOn")]
        public DateTime DateCreated { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? DateLastModified { get; set; }
        public int? DeletedBy { get; set; }
        [Column("ExpirationDate")]
        public DateTime? DateDeleted { get; set; }


        public virtual StaffMemberEntity StaffMember { get; set; }
        public virtual ChildProfileEntity ChildProfile { get; set; }
        //public virtual MemberEntity VerifiedByMember { get; set; }
        //public virtual PropagatingStaffMemberEntity PropagatingStaffMember { get; set; }

        //public virtual PropagatingChildProfileEntity PropagatingChildProfile { get; set; }
        //public virtual AppointmentOccurrenceTypeEntity AppointmentOccurrenceType { get; set; }
        //public virtual AppointmentTypeEntity AppointmentType { get; set; }
        //public virtual AppointmentCancellationTypeEntity AppointmentCancellationType { get; set; }
        //public virtual PropagatingFunderEntity PropagatingFunder { get; set; }
        public virtual FunderEntity Funder { get; set; }
        //public virtual ProviderServiceLineEntity ProviderServiceLine { get; set; }
        //public virtual PropagatingProviderServiceEntity PropagatingProviderService { get; set; }
        public virtual ProviderServiceEntity ProviderService { get; set; }

        //public virtual StaffMemberEntity SessionNoteReviewedByStaff { get; set; }
        //public virtual AppointmentCustomTagEntity AppointmentActivityTag { get; set; }
        //public virtual AppointmentCustomTagEntity AppointmentCancellationTag { get; set; }
        //public virtual PropagatingChildProfileAuthorizationEntity PropagatingChildProfileAuthorization { get; set; }

        //public virtual IEnumerable<ClaimAppointmentLinkEntity> EncounterAppointmentLinks { get; set; }
        //public virtual AddressEntity Address { get; set; }
        //public virtual AddressEntity StartingAddress { get; set; }
        //public virtual AddressEntity EndingAddress { get; set; }


        public virtual LocationCodeEntity PlaceOfService { get; set; }
        public virtual ProviderLocationEntity Location { get; set; }
        public virtual ProviderServiceLineEntity ProviderServiceLine { get; set; }
        public virtual ProviderBillingCodeCredentialEntity ProviderBillingCodeCredential { get; set; }

        public virtual ICollection<WorkflowHistoryEntity> WorkflowHistories { get; set; }
        [NotMapped]
        public virtual WorkflowHistoryEntity WorkflowHistory { get; set; }
        [NotMapped]
        public virtual ICollection<ClaimAppointmentLinkEntity> ClaimAppointmentLinks { get; set; }
        public virtual ChildProfileAuthorizationBillingCodeEntity ChildProfileAuthorizationBillingCode { get; set; }
        public virtual PropagatingAccountInfoEntity PropagatingAccountInfo { get; set; }
        //public virtual PropagatingChildProfileFunderEntity PropagatingChildProfileFunder { get; set; }

        public virtual ProviderBillingCodeEntity ProviderBillingCode { get; set; }
        public virtual MemberEntity ModifiedByMember { get; set; }
        //public virtual ICollection<TrialSetEntity> TrialSets { get; set; }
        //public virtual ICollection<BehaviorPlanDataRecordingEntity> BehaviorPlanDataRecordings { get; set; }
        //public virtual ICollection<AppointmentSessionCommentEntity> SessionNotes { get; set; }

        //public virtual List<AppointmentEntity> SeriesAppointments { get; set; }
    }
}