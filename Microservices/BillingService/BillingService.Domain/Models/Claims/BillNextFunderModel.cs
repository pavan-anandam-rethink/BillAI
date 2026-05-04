using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models.Claims
{
    [ExcludeFromCodeCoverage]
    public class BillNextFunderModel : UserInfo
    {
        public int claimId { get; set; }
        public int secondaryFunderId { get; set; }
        public string controlNumber { get; set; }
        public bool isClaimLevelAdjustment { get; set; }
    }
}
