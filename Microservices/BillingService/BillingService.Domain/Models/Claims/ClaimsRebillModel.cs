using Rethink.Services.Common.Enums.Billing;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models.Claims
{
    [ExcludeFromCodeCoverage]
    public class ClaimsRebillModel
    {
        public int[] ClaimIds { get; set; }
        public string RebillReason { get; set; }
        public int SubmissionReasonCode { get; set; }
        public string Note { get; set; }
        public string ClaimNote { get; set; }
        
    }
    public class SecondaryBillingClaimsRebillModel : RebillIdWithUserInfo
    {       
        public bool IsSecondary { get; set; } = false;
        public AdjustmentLevel? AdjustmentLevel { get; set; }
        public List<SecondaryFunderDetailsModel> SecondaryFunderDetails { get; set; }


    }
    
}
