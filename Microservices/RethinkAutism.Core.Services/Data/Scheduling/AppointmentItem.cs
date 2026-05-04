using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Rethink.Services.Common.Models;
using RethinkAutism.Contracts.DataObjects.Curriculum;

namespace RethinkAutism.Core.Services.Data.Scheduling
{
    public class AppointmentItem
    {
        public int Id { get; set; }

        public int StaffId { get; set; }

        public int MemberId { get; set; }

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

        public int? EVVStatusId { get; set; }

        public string EVVStatusName { get; set; }

        public string EVVRejectedReason { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int StartTime { get; set; }

        public int EndTime { get; set; }

        public int? ActualStartTime { get; set; }

        public int? ActualEndTime { get; set; }

        public int ScheduledById { get; set; }

        public DateTime DateCreated { get; set; }

        public int AppointmentTypeId { get; set; }

        public IList<int> DayTypes { get; set; }

        public int MonthDay { get; set; }

        public int? MonthTypeId { get; set; }

        public int? MonthOccurrenceTypeId { get; set; }

        public int? MonthOccurrenceDayId { get; set; }

        public string AppointmentTypeName { get; set; }

        public DateTime? StaffVerificationDate { get; set; }

        public DateTime? ClientVerificationDate { get; set; }

        public DateTime? PrincipalVerificationDate { get; set; }

        public int? FunderId { get; set; }

        public int? EVVClearingHouseId { get; set; }

        public int? StateId { get; set; }

        public string FunderName { get; set; }

        public DateTime? ClientFunderStart { get; set; }

        public DateTime? ClientFunderEnd { get; set; }

        public string BillingCode { get; set; }

        public int? ServiceId { get; set; }

        public int? ProviderServiceId { get; set; }

        public string ServiceName { get; set; }

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

        public byte[] ParentSignature { get; set; }

        public IList<DayPilotEvent> Events { get; set; }

        public int SessionNoteResponseId { get; set; }

        public int? SeriesAppointmentId { get; set; }

        public DateTime? OccurrenceEndDate { get; set; }

        public int? ProviderBillingCodeId { get; set; }

        public int? ProviderBillingCodeCredentialId { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public DateTime? SeriesAppointmentStartDate { get; set; }

        public DateTime? InitOccurrenceStartDate { get; set; }

        public bool? IsParentVerificationRequired { get; set; }

        public bool? IsSessionNoteEnteredRequired { get; set; }

        public int? SessionNoteReviewedBy { get; set; }

        public decimal? AuthorizationUsedHours { get; set; }

        public string ActivityTagName { get; set; }

        public int CancellationTypeId { get; set; }

        public int? CancellationTagId { get; set; }

        public bool ExceedAuthorizedHours { get; set; }

        public bool? CheckExceedAuthorizationHours { get; set; }

        public int? ProcedureCodeIdPreviousReference { get; set; }

        public string Notes { get; set; }

        public string ClientContactName { get; set; }

        public string ClientContactRelationship { get; set; }

        public int PaycodeId { get; set; }

        public string Paycode { get; set; }

        public string PaycodeName { get; set; }

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

        public ClientAddress Address { get; set; }

        public DateTime? DateBillingReported { get; set; }

        public DateTime? DatePayrollReported { get; set; }

        public int ModifiedBy { get; set; }

        public DateTime DateLastModified { get; set; }

        public string Location { get; set; }

        public string Status { get; set; }

        public DateTime? DateDoeBilled { get; set; }

        public string StatusDoeBilled { get; set; }

        public int? SessionNoteDraftResponseId { get; set; }

        public bool? NoAuthRequired { get; set; }

        public DateTime? AuthorizationEndDate { get; set; }

        public DateTime? AuthorizationStartDate { get; set; }

        public double AuthorizedWeeklyHours { get; set; }

        public bool OccurrenceLookup { get; set; }

        public bool IsClientDemo { get; set; }

        public string Validation { get; set; }

        public string AuthorizationNumber { get; set; }

        public int Units { get; set; }

        public string RenderingProvider { get; set; }

        public IList<EVVReason> EVVReasons { get; set; }

        public IList<AcknowledgeableException> AcknowledgeableExceptions { get; set; }

        public int? EVVActionTakenId { get; set; }

        public bool IsEVV { get; set; }

        public decimal? ClockInLatitude { get; set; }

        public decimal? ClockInLongitude { get; set; }

        public decimal? ClockOutLatitude { get; set; }

        public decimal? ClockOutLongitude { get; set; }

        public decimal? ToLocationLatitude { get; set; }

        public decimal? ToLocationLongitude { get; set; }

        public string ClockInAddress { get; set; }

        public string ClockOutAddress { get; set; }

        public string Modifier1 { get; set; }

        public string Modifier2 { get; set; }

        public DateTime LastModified { get; set; }

        public bool IsMissingBillingInfo { get; set; }

        public bool IsPendingSubmission { get; set; }

        public int? ClearingHouseId { get; set; }

        public string Postfix { get; set; }

        public int? staffRecordInd { get; set; }

        public int? parentRecordInd { get; set; }

        public bool? IsForAuthCalc { get; set; }

        public string CancellationNote { get; set; }

        public int StaffAddressId { get; set; }

        public int? FunderBillingCodeId { get; set; }

        // additional fields used in query to get appointment items from DB
        [JsonIgnore]
        public DateTime? EndDateInitial { get; set; }

        [JsonIgnore]
        public DateTime? StaffVerificationDateInitial { get; set; }

        [JsonIgnore]
        public DateTime? ClientVerificationDateInitial { get; set; }

        [JsonIgnore]
        public int DayTypesInitial { get; set; }

        [JsonIgnore]
        public string SupervisorFirstName { get; set; }

        [JsonIgnore]
        public string SupervisorLastName { get; set; }

        [JsonIgnore]
        public int PropagatingProviderServiceId { get; set; }

        [JsonIgnore]
        public DateTime? PropagatingProviderServiceEndDate { get; set; }
    }
}
