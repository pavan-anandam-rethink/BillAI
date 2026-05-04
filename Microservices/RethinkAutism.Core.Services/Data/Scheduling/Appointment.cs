using RethinkAutism.Contracts.DataObjects.Curriculum;
//using RethinkAutism.Core.Services.Base;
using System;
using System.Collections.Generic;


namespace RethinkAutism.Core.Services.Data.Scheduling
{
    public class Appointment : AppointmentItem
    {
        public string Description { get; set; }

        public string SignatureParentName { get; set; }

        public bool? VerifiedFromSessionNote { get; set; }

        public int ClientContactId { get; set; }

        public string ClientLocation { get; set; }

        public string CancellationTypeName { get; set; }

        public DateTime RescheduleDueDate { get; set; }

        public int RescheduleAssignedToId { get; set; }

        public int SessionNoteFormId { get; set; }

        public IList<SessionNoteComment> SessionNoteComments { get; set; }

        public byte[] StaffSignature { get; set; }

        public byte[] ClientSignature { get; set; }

        public byte[] PrincipalSignature { get; set; }

        public int AssociatedAppointmentId { get; set; }

        public int DiagnosisId { get; set; }

        //public Address StartingAddress { get; set; }

        //public Address EndingAddress { get; set; }

        public DateTime? SessionNoteReviewedOn { get; set; }

        public string SessionNoteReviewedByName { get; set; }

        public string SessionNoteReviewedByTitle { get; set; }

        public int? CopaymentMethodId { get; set; }

        public string CopaymentReferenceNumber { get; set; }

        public decimal? CopaymentAmountCollected { get; set; }

        public string CancellationTagName { get; set; }

        public bool CheckAuthorizationExceed { get; set; }

        public int? VerifiedById { get; set; }

        public string VerifiedByName { get; set; }

        public string VerifiedByTitle { get; set; }

        public int? RescheduleFromId { get; set; }

        public DateTime? InitAppointmentStartDate { get; set; }

        public int? InitAppointmentStartTime { get; set; }

        public int? InitAppointmentEndTime { get; set; }

        public bool? MissingSessionNotesFileCabinet { get; set; }

        public int? SessionNoteDraftFormId { get; set; }

        public DateTime? SessionNoteDraftOn { get; set; }

        public int? SessionNoteDraftStaffMemberId { get; set; }

        public string SessionNoteDraftStaffMemberName { get; set; }

        public bool LinkedToEncounter { get; set; }

        public bool LinkedToApprovedEncounter { get; set; }

        public bool? HasTrialSetData { get; set; }

        public bool? HasBehaviorPlanData { get; set; }

        public bool ProviderPrincipalSignature { get; set; }

        public DateTime? SessionNoteSubmitOn { get; set; }

        public string SessionNoteSubmitBy { get; set; }

        public string SessionNoteStatus { get; set; }
        public int? ClientFunderId { get; set; }

        public bool HasDiscrepancy { get; set; }

        public bool SessionNoteDraftAutoSaved { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class AppointmentShort
    {
        public int Id { get; set; }
        public string ActivityTagName { get; set; }
        public int? ActualStartTime { get; set; }
        public int? ActualEndTime { get; set; }
        public DateTime? AdminVerificationDate { get; set; }
        public int AppointmentTypeId { get; set; }
        public DateTime? AuthorizationEndDate { get; set; }
        public DateTime? AuthorizationStartDate { get; set; }
        public int? CancellationTypeId { get; set; }
        public bool? CheckExceedAuthorizationHours { get; set; }
        public int? ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientShortName { get; set; }
        public DateTime? ClientVerificationDate { get; set; }
        public string EVVStatusName { get; set; }
        public int? EVVStatusId { get; set; }
        public int EndTime { get; set; }
        public int FrequencyInterval { get; set; }
        public bool? IsParentVerificationRequired { get; set; }
        public bool? IsSessionNoteEnteredRequired { get; set; }
        public DateTime? OccurrenceEndDate { get; set; }
        public int OccurrenceFrequency { get; set; }
        public int OccurrenceTypeId { get; set; }
        public string ParentVerifiedAddress { get; set; }
        public int? ProcedureCodeId { get; set; }
        public DateTime? PropagatingServiceEndDate { get; set; }
        public string PropagatingServiceName { get; set; }
        public string ProviderServiceName { get; set; }
        public string ServiceName { get; set; }
        public int? SessionNoteResponseId { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public string StaffSupervisorName { get; set; }
        public DateTime? StaffVerificationDate { get; set; }
        public string StaffVerifiedAddress { get; set; }
        public DateTime StartDate { get; set; }
        public int StartTime { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public DateTime? SeriesAppointmentStartDate { get; set; }
        public IList<int> DayTypes { get; set; }
        public int? SeriesAppointmentId { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string SignatureParentRelationship { get; set; }
        public Guid? ClientSignatureId { get; set; }
        public bool OccurrenceLookup { get; set; }
        public DateTime? InitOccurrenceStartDate { get; set; }
        public int? FunderId { get; set; }
        public IList<DayPilotEvent> Events { get; set; }
        public int MonthDay { get; set; }
        public int? MonthTypeId { get; set; }
        public int? MonthOccurrenceTypeId { get; set; }
        public int? MonthOccurrenceDayId { get; set; }
        public int? ServiceId { get; set; }
        public int? ProviderServiceId { get; set; }
        public int? ProviderBillingCodeId { get; set; }
        public decimal? AuthorizationUsedHours { get; set; }
        public int? ProviderBillingCodeCredentialId { get; set; }
        public decimal? ParentLatitude { get; set; }
        public decimal? ParentLongitude { get; set; }
        public double AuthorizedWeeklyHours { get; set; }
        public string ClientInitials { get; set; }
        public string StaffInitials { get; set; }
        public DateTime DateLastModified { get; set; }
    }
}