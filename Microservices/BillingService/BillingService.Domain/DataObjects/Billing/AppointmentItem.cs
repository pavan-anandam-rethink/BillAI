using System;

namespace BillingService.Domain.DataObjects.Billing
{
    public class AppointmentItem
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public string StaffInitials { get; set; }
        public string StaffTitle { get; set; }
        public string StaffLocation { get; set; }
        public string StaffSupervisorName { get; set; }
        public int? ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientInitials { get; set; }
        public string ClientShortName { get; set; }
        public int OccurrenceTypeId { get; set; }
        public int OccurrenceFrequency { get; set; }
        public int FrequencyInterval { get; set; }
        public string OccurrenceTypeName { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public int? ActualStartTime { get; set; }
        public int? ActualEndTime { get; set; }
        public int ScheduledById { get; set; }
        public DateTime ScheduledOn { get; set; }
        public int AppointmentTypeId { get; set; }
        //public IList<int> DayTypes { get; set; }
        public int MonthDay { get; set; }
        public int? MonthTypeId { get; set; }
        public int? MonthOccurrenceTypeId { get; set; }
        public int? MonthOccurrenceDayId { get; set; }
        public string AppointmentTypeName { get; set; }
        public DateTime StaffVerificationDate { get; set; }
        public DateTime ClientVerificationDate { get; set; }
        public DateTime? PrincipalVerificationDate { get; set; }
        public int FunderId { get; set; }
        public string FunderName { get; set; }
        public int? ServiceId { get; set; }
        public int? ProviderServiceId { get; set; }
        public string ServiceName { get; set; }
        public string ServiceLocation { get; set; }
        public string ProviderServiceName { get; set; }
        public string PropagatingServiceName { get; set; }
        public DateTime? PropagatingServiceEndDate { get; set; }
        public int? FromLocationId { get; set; }
        public string FromLocation { get; set; }
        public int? ToLocationId { get; set; }
        public string ToLocation { get; set; }
        public string LocationName { get; set; }
        public decimal? Mileage { get; set; }
        public int ProcedureCodeId { get; set; }
        public Guid? StaffSignatureId { get; set; }
        public Guid? ClientSignatureId { get; set; }
        public Guid? PrincipalSignatureId { get; set; }
        //public IList<DayPilotEvent> Events { get; set; }
        public int SessionNoteResponseId { get; set; }
        public int? SeriesAppointmentId { get; set; }
        public DateTime? OccurrenceEndDate { get; set; }
        public int? ProviderBillingCodeId { get; set; }
        public int? ProviderBillingCodeCredentialId { get; set; }
        public string BillingCode { get; set; }
        public string BillingCode2 { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime? SeriesAppointmentStartDate { get; set; }
        public bool? IsParentVerificationRequired { get; set; }
        public bool? IsSessionNoteEnteredRequired { get; set; }
        public int? SessionNoteReviewedBy { get; set; }
        public decimal? AuthorizationUsedHours { get; set; }
        public string ActivityTagName { get; set; }
        public int CancellationTypeId { get; set; }
        public bool ExceedAuthorizedHours { get; set; }
        public bool? CheckExceedAuthorizationHours { get; set; }
        public int? ProcedureCodeIdPreviousReference { get; set; }
        public string Notes { get; set; }
        public string ClientContactName { get; set; }
        public string ClientContactRelationship { get; set; }
        public int PaycodeId { get; set; }
        public string Paycode { get; set; }
        public string SignatureParentRelationship { get; set; }
        public int? LocationId { get; set; }
        public DateTime? AdminVerificationDate { get; set; }
        public int? AdminVerifiedBy { get; set; }
        public decimal? ParentLatitude { get; set; }
        public decimal? ParentLongitude { get; set; }
        public decimal? StaffMemberLatitude { get; set; }
        public decimal? StaffMemberLongitude { get; set; }
        public string ParentVerifiedAddress { get; set; }
        public string StaffVerifiedAddress { get; set; }
        public int? ActivityTagId { get; set; }

        public DateTime? DateBillingReported { get; set; }
        public DateTime? DatePayrollReported { get; set; }
        public int ModifiedBy { get; set; }
        public string ModifiedByName { get; set; }
        public DateTime DateLastModified { get; set; }

        public string Location { get; set; }
        //public string Status { get; set; }

        public int? SessionNoteDraftResponseId { get; set; }

        public DateTime? AuthorizationEndDate { get; set; }
        public DateTime? AuthorizationStartDate { get; set; }
        public double AuthorizedWeeklyHours { get; set; }
        public bool OccurrenceLookup { get; set; }
    }
}
