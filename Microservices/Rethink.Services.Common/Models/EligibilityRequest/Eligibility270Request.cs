using System;

namespace Rethink.Services.Common.Models.EligibilityRequest
{
    public class Eligibility270Request
    {
        public string? FunderName { get; set; }
        public string? ClientName { get; set; }
        public int? ClientId { get; set; }
        public string? ChildProfileReferringProviderId { get; set; }
        public string? ChildProfileReferringProviderName { get; set; }
        public int? ChildProfileRenderingProviderId { get; set; }
        public string? ChildProfileRenderingProviderName { get; set; }
        public string? StaffMemberNpiNumber { get; set; }
        public string? ClearingHousePayerName { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? DOB { get; set; }       
        public int? Id { get; set; }
        public int? AccountInfoId { get; set; }
        public string SubscriberId { get; set; }
        public int? ClientFunderId { get; set; }
        public int? FunderId { get; set; }
        public int? GenderId { get; set; }
        public int? MemberId { get; set; }
        public int? ClearingHouseId { get; set; }
        public string ClearingHouseTitle { get; set; }
        public string PayerId { get; set; }
    }
}
