using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Enums.BH;
using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models.Clients
{
    [Owned]
    public class ClientAuthorizationModel
    {
        public bool IsActive { get; set; }
        public int Id { get; set; }
        public string AuthorizationNumber { get; set; }
        public int? AuthorizationSubmissionTypeId { get; set; }
        public int FunderId { get; set; }
        public int? FunderAppointmentExceedingAuthorizationAlertId { get; set; }
        public int? RenderingProviderId { get; set; }
        public int? RenderingProviderTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? InactiveDate { get; set; }

        public int? ReferringProviderId { get; set; }
        public bool? ReferringProviderIsActive { get; set; }
        public int? ServiceProviderId { get; set; }
        public int? BillingProviderId { get; set; }

        public int? ServiceLineId { get; set; }
        public List<ClientAuthorizationDiagnosisCodeModel> DiagnosisCodes { get; set; }

        public int? TotalNumberOfUnits { get; set; }
        public int AuthorizationDistributionTypeId { get; set; }
        public List<ClientAuthorizationBillingCodeModel> BillingCodes { get; set; }

        public int? ChildProfileFunderServiceLineMappingId { get; set; }
        public bool ChildProfileFunderMappingIsActive { get; set; }
        public int? ChildProfileFunderMappingId { get; set; }
        public int? ShowAuthorizationByTypeId { get; set; }
        public int AppointmentsCount { get; set; }
        public List<ClientAuthorizationAppointmentModel> AppointmentsInfo { get; set; }

        public bool IsStartDateValid { get; set; }
        public bool IsEndDateValid { get; set; }
        public bool IsInactiveDateValid { get; set; }
        public bool IsFunderValid { get; set; }
        public int? DiactivatedById { get; set; }
        public string RenderingProviderName { get; set; }
        public string AccountOrganizationName { get; set; }
        public PropagatingRenderingProviderData PropagatingRenderingProviderData { get; set; }
    }
    [Owned]
    public class ClientAuthorizationDiagnosisCodeModel
    {
        public int Id { get; set; }
        public int DiagnosisId { get; set; }
        public string DiagnosisCode { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public bool IncludeOnClaims { get; set; }
        public bool IsActive { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string DiagnosisFullDescription { get; set; }
    }
    [Owned]
    public class ClientAuthorizationAppointmentModel
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string FunderName { get; set; }
        public string ServiceName { get; set; }
        public int? ProcedureCodeId { get; set; }
        public int FrequencyInterval { get; set; }
        public int UnitType { get; set; }
        public int? EventMinutes { get; set; }
    }
    [Owned]
    public class ClientAuthorizationBillingCodeModel
    {
        public int Id { get; set; }
        public int BillingCodeId { get; set; }
        public int UnitTypeId { get; set; }
        public string FrequencyType { get; set; }
        public int NoOfUnits { get; set; }
        public int? Interval { get; set; }
        public int? SchedulingGoalNoOfUnits { get; set; }
        public double? SchedulingGoalNoOfUnitsCalculated { get; set; }
        public double? SchedulingGoalHoursCalculated { get; set; }
        public double? TotalScheduledGoals { get; set; }
        public double? TotalScheduledGoalsUnit { get; set; }
        public double TotalHours { get; set; }
        public double TotalUnits { get; set; }
        public double RemainingHours { get; set; }
        public double RemainingUnits { get; set; }
        public FrequencyTypes? SchedulingGoalFrequencyTypeId { get; set; }
        public int? SchedulingGoalInterval { get; set; }
        public int? ChildProfileAuthorizationId { get; set; }
        public BillingCodeData ProviderBillingCode { get; set; }
        public ClientAuthorization ChildProfileAuthorization { get; set; }
        public BillingCodeData AppointmentProviderBillingCode { get; set; }
    }
    [Owned]
    public class BillingCodeModel
    {
        public int Id { get; set; }
        public string BillingCodeText { get; set; }
        public decimal? Rate { get; set; }
        public int UnitTypeId { get; set; }
        public string billingCode { get; set; }
        public int? Unit { get; set; }
        public int? RateTypeId { get; set; }
        public int? RoundingTypeId { get; set; }
        public string BillingCode2 { get; set; }
        public decimal? Rate2 { get; set; }
        public int? UnitTypeId2 { get; set; }
        public int? RoundingTypeId2 { get; set; }
        public int? ProviderServiceId { get; set; }
        public bool? RestrictStaffProviderToService { get; set; }
        public string Description { get; set; }
        public int ServiceId { get; set; }
        public int? FrequencyTypeId { get; set; }
        public string Modifier { get; set; }
        public bool? Combined { get; set; }
        public int? BillingCodeTemplateId { get; set; }
        public int? DurationTypeId { get; set; }
        public int? Duration { get; set; }
        public DateTime? PropagatingEndDate { get; set; }
        public bool? NoAuthRequired { get; set; }
        public int? RenderingProviderTypeId { get; set; }
        public int? RenderingProviderStaffId { get; set; }

        public ProviderServiceModel ProviderService { get; set; }
    }
    [Owned]
    public class ProviderServiceModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal BaseRate { get; set; }
    }
    [Owned]
    public class PropagatingRenderingProviderData
    {
        public DateTime? DateLastModified { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? RenderingProviderEndDate { get; set; }
        public int? LastSelectionTypeId { get; set; }
        public string InfoMessage { get; set; }
    }
    [Owned]
    public class ClientAuthDiagnosisCodesModel
    {
        public int total { get; set; }
        public List<ClientAuthDiagnosisCodes> data { get; set; }
    }
    [Owned]
    public class ClientAuthDiagnosisCodes
    {
        public int childProfileAuthorizationId { get; set; }
        public int diagnosisId { get; set; }
        public int order { get; set; }
        public bool includeOnClaims { get; set; }
        public int childProfileDiagnosisId { get; set; }
        public int id { get; set; }
        public MetaData metadata { get; set; }
    }
}
