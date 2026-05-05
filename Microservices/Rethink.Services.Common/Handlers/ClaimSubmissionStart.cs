using Rethink.Services.Common.Enums.Billing;
using System.Diagnostics.CodeAnalysis;

namespace Rethink.Services.Common.Handlers
{
    [ExcludeFromCodeCoverage]
    public class ClaimSubmissionStart
    {
        public int ClaimId { get; set; }
        public int MemberId { get; set; }
        public int AccountInfoId { get; set; }
        public int ClearingHouseId { get; set; }
        public int PendingClaimSubmissionId { get; set; }
        public AdjustmentLevel? AdjustmentLevel { get; set; }
        public bool IsSecondary { get; set; } = false;
    }

    [ExcludeFromCodeCoverage]
    public class ChClaimValidation
    {
        public int ClaimId { get; set; }
    }

}
