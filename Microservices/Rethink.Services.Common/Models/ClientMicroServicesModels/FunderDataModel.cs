using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class FunderDataDetails
    {
        public FunderDataModel funder { get; set; }
    }

    [Owned]
    public class FunderDataDetailsList
    {
        public int Total { get; set; }
        public List<FunderDataModel> Data { get; set; }
    }

    [Owned]
    public class FunderDataModel
    {
        public int id { set; get; }
        public string funderName { get; set; }
        public string phone { get; set; }
        public int funderTypeId { get; set; }
        public bool referringProviderRequiredOnClaim { get; set; }
        public int billingProviderOptionId { get; set; }
        public string fax { get; set; }
        public string emailAddress { get; set; }
        public int accountId { get; set; }
        public int addressId { get; set; }
        public string description { get; set; }
        public bool isActive { get; set; }
        public string vendorId { get; set; }
        public string note { get; set; }
        public bool billingCombineCharges { get; set; }
        public int appointmentDuplicateClientTimeAlertId { get; set; }
        public int appointmentDuplicateClientServiceAlertId { get; set; }
        public int appointmentMissingBillingDataAlertId { get; set; }
        public int appointmentExceedingAuthorizationAlertId { get; set; }
        public int providerLocationId { get; set; }
        public int? funderCoverageTypeId { get; set; }
        public int kareoInsuranceCompanyId { get; set; }
        public int combineChargeTypeId { get; set; }
        public int clearingHousePayerId { get; set; }
        public string chPayerId { get; set; }
        public string clearingHousePayerName { get; set; }
        public bool allowOverlappingAppointments { get; set; }
        public int appointmentExpiredCertificationAlertId { get; set; }
        public int electronicVisitVendorId { get; set; }
        public bool includeKareoSvcApptTime { get; set; }
        public long evvBusinessEntityId { get; set; }
        public string evvUserId { get; set; }
        public string evvPassword { get; set; }
        public bool sessionTimeToMilitaryTime { get; set; }
        public bool isDph { get; set; }
        public int stateId { get; set; }
        public string? payerId { get; set; }
        public string programId { get; set; }
        public int jurisdictionId { get; set; }
        public int medicaidNumberId { get; set; }
        public string hhaxClientId { get; set; }
        public string hhaxClientSecret { get; set; }

        public int claimCreationFrequency { get; set; } = 1;
        public string? selectedDays { get; set; }
        public int? frequency { get; set; }
        public string? time { get; set; }
        public int? timeZone { get; set; }
        [JsonPropertyName("timeZoneData")]
        public TimeZoneDataModel TimeZoneData { get; set; }

        [NotMapped]
        public ClientAddress address { get; set; }
        public MetaData metaData { get; set; }
        [NotMapped]
        public List<ServiceFunderData> ServiceFunders { get; set; }
        [NotMapped]
        public List<FunderInsurancePlan> FunderInsurancePlans { get; set; }

        public InsuranceContacts InsuranceContacts { get; set; }
    }

    public class FunderDataList
    {
        public List<FunderDataModel> funders { get; set; }
    }
}
