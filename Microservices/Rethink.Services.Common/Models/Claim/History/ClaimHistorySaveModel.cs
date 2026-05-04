using Rethink.Services.Common.Enums.Billing;
using System;

namespace Rethink.Services.Common.Models.Claim.History
{
    public class ClaimHistorySaveModel
    {
        public int ClaimId { get; set; }
        public int MemberId { get; set; }
        public ClaimActionMode Mode { get; set; }
        public ClaimAction ClaimAction { get; set; }
        public ClaimHistoryAction ClaimHistoryAction { get; set; }
        public DateTime? ActionDate { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string? ImpersonationUserName { get; set; }
    }
}
