using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;

namespace BillingService.Domain.Models.BillingSettings
{
    public class BillingFunderSettingResponseModel
    {
        public List<ClaimFilingIndicatorModel> ClaimFilingIndicator { get; set; }
        public List<BillingFunderSettings> Data { get; set; }
        public Dictionary<int,string> TimeZone { get; set; }
        public int Total { get; set; }
        public bool ShowEligblity { get; set; } = false;
    }

    public class BillingFunderSettings
    {
        public int Id { get; set; }
        public int FunderId { get; set; }
        public string FunderName { get; set; }
        public string ClearingHousePayerName { get; set; }
        public string ClearingHousePayerId { get; set; }
        public string InsuranceType { get; set; }
        

    }
    public class BillingFunderSettingAPIResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class BillingSettingInformationModel
    {
        public int PayToAddressOverrideOption { get; set; }
        public string? CompanyName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? ZipExtension { get; set; }
        public string? DunningMessage { get; set; }
        public string? GlobalMessage { get; set; }

    }

    public class SaveBillingSettingRequest
    {
        public int AccountId { get; set; }
        public int PayToAddressOverrideOption { get; set; }
        public string? CompanyName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? ZipExtension { get; set; }
        public string? DunningMessage { get; set; }
        public string? GlobalMessage { get; set; }
    }

}
