using Microsoft.EntityFrameworkCore;
using System;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class AppointmentRethinkModel
    {
        public int id { get; set; }
        public DateTime startDateTime { get; set; }
        public DateTime? endDateTime { get; set; }
        public int? locationId { get; set; }
        public bool isRecurring { get; set; }
        public string? recurrenceType { get; set; }
        public int? modifiedBy { get; set; }
        public int appointmentTypeId { get; set; }
        public int staffId { get; set; }
        public int? clientId { get; set; }
        public int procedureCodeId { get; set; }
        public int serviceId { get; set; }
        public int providerServiceId { get; set; }
        public int funderId { get; set; }
        public int? actualStartTime { get; set; }
        public int? actualEndTime { get; set; }
        public int? providerBillingCodeId { get; set; }
        //public int? clientFunderId { get; set; } // #DBMIGRATION
        public int providerBillingCodeCredentialId { get; set; }
        public DateTime? adminVerificationDate { get; set; }
        public DateTime? staffVerificationDate { get; set; }
        public DateTime? clientVerificationDate { get; set; }
        public int? sessionNoteResponseId { get; set; }
        public int facilityId { get; set; }
        public int clientAccountInfoId { get; set; }
        public int authorizationRenderingProviderTypeId { get; set; }
        public int memberId { get; set; }
        public int staffAccountInfoId { get; set; }

        // missing columns
        //<
        //public int? propagatingClientAuthRenderingProviderId { get; set; } // #DBMIGRATION
        public int? propagatingClientAuthReferringProviderId { get; set; }
        public int? propagatingProviderBillingCodeId { get; set; }
        public int? propagatingAccountInfoId { get; set; }
        public string appointmentDescription { get; set; }
        public int toLocationId { get; set; }
        public string toLocation { get; set; }

        public int occurrenceTypeId { get; set; }

        public int diagnosisId { get; set; }
        public int? propagatingStaffMemberId { get; set; }
        public int workflowHistoryId { get; set; }
        public DateTime? DateDeleted { get; set; }
        //public int DayTypes { get; set; }
        public AppointmentClientAuthBillingCodeModel? ChildProfileAuthorizationBillingCode { get; set; }
        public ProviderBillingCodeCredentialModel ProviderBillingCodeCredential { get; set; }
        public BillingCodeData ProviderBillingCode { get; set; }
        public RethinkStaffMember StaffMember { get; set; }
        public ClientUserModel ChildProfile { get; set; }
        public FunderDataModel Funder { get; set; }
        public LocationCodesModel PlaceOfService { get; set; }
        public AppointmentWorkFlowHistoyModel WorkFlowHistory { get; set; }
        public ClientProviderServiceModel ProviderService { get; set; }
        public ChildProfileServiceLines ProviderServiceLine { get; set; }
        public ProviderLocations Location { get; set; }
        public int startTime_Calculate { get; set; }
        public int endTime_Calculate { get; set; }
        public DateTime startDate_Calculate { get; set; }
        public DateTime? endDate_Calculate { get; set; }
        public DateTime startDate
        {
            set
            {
                startDate_Calculate = startDateTime.Date;
            }
            get
            {
                return startDateTime.Date;
            }
        }
        public DateTime? endDate
        {
            set
            {
                endDate_Calculate = endDateTime?.Date;
            }
            get
            {
                return endDateTime?.Date;
            }
        }
        public int startTime
        {
            set
            {
                startTime_Calculate = (int)startDateTime.TimeOfDay.TotalMinutes;
            }
            get
            {
                return (int)startDateTime.TimeOfDay.TotalMinutes;
            }
        }
        public int endTime
        {
            set
            {
                endTime_Calculate = (int)endDateTime?.TimeOfDay.TotalMinutes;
            }
            get
            {
                return (int)endDateTime?.TimeOfDay.TotalMinutes;
            }
        }
    }
    [Owned]
    public class ChildProfileAuthorizationBillingCodeModel
    {
        public int childProfileAuthorizationId { get; set; }
        public int providerBillingCodeId { get; set; }
        public int noOfUnits { get; set; }
        public int unitTypeId { get; set; }
        public int frequencyTypeId { get; set; }
        public int schedulingGoalNoOfUnits { get; set; }
        public int schedulingGoalFrequencyTypeId { get; set; }
        public int providerServiceId { get; set; }
        public int id { get; set; }
        public ClientAuthorization ChildProfileAuthorization { get; set; }
        public BillingCodeData ProviderBillingCode { get; set; }
        public MetaData metaData { get; set; }
    }
}
