using Rethink.Services.Common.Enums.Billing;

namespace Rethink.Services.Common.Models.Claim
{
    public class ClaimSubmitModel
    {
        public int Id { get; set; }
        public int AccountInfoId { get; set; }
        public int MemberId { get; set; }
        public int? ClearinghouseId { get; set; }
        public ClaimStatus ClaimStatus { get; set; }
        public ClaimFrequencyType? FrequencyTypeId { get; set; }
        public int PendingClaimSubmissionId { get; set; }
        public AdjustmentLevel? AdjustmentLevel { get; set; }
        public bool IsSecondary { get; set; } = false;
    }
}
