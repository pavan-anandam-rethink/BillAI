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
        public List<FeatureStatusDto> BillingFeatures { get; set; } = [];
    }
}
