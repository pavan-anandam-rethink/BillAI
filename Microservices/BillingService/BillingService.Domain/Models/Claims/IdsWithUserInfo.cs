using System.Collections.Generic;

namespace BillingService.Domain.Models.Claims
{
    public class IdsWithUserInfo : UserInfo
    {
        public int[] Ids { get; set; }
    }

    public class DeleteClaimsInfo : IdsWithUserInfo
    {
        public string? ImpersonationUserName { get; set; }
    }

    public class FlagClaimsRequest : UserInfo
    {
        public List<int> ClaimIds { get; set; } = new();
        public List<FlagReasonRequest> Reasons { get; set; } = new();
        public string? Notes { get; set; }

        // Optional: for edit mode
        public int? ClaimFlagTransactionId { get; set; }
        public string ImpersonationUserName { get; set; } = string.Empty;
    }

    public class FlagReasonRequest
    {
        public int ReasonId { get; set; }
    }

    public class RebillIdWithUserInfo : UserInfo
    {
        public int ClaimId { get; set; }
    }

    public class UnflagImperson : IdsWithUserInfo
    {
        public string Rethinkuser { get; set; }  = string.Empty;

    }
}
