using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models.Claims
{
    [ExcludeFromCodeCoverage]
    public class ClaimProcessRequestModel
    {
        public string BatchId { get; set; }
        public ClaimsSubmitModel RequestModel { get; set; }
        public int TotalClaims { get; set; }
        public string? ClaimStatus { get; set; }
    }
    [ExcludeFromCodeCoverage]
    public class ClaimApproveRequestModel
    {
        public string BatchId { get; set; }
        public IdsWithUserInfo RequestModel { get; set; }
        public int TotalClaims { get; set; }
    }
}
