using System.Collections.Generic;

namespace BillingService.Domain.Models.BillingSettings
{
    public class BillingFunderSettingRequestModel
    {
        public int AccountInfoId { get; set; }
        public int? FunderId { get; set; }
        public string FunderName { get; set; } = string.Empty;
        public int? ClaimFilingIndicatorId { get; set; }
        public bool IncludeTaxonomyCode { get; set; } = false;
        public bool? Is837PEnrollmentRequired { get; set; }
        public bool? Is837PEnrollmentCompleted { get; set; }
        public List<FeatureStatusDto> BillingFeatures { get; set; } = [];
    }
}
