using Rethink.Services.Common.Enums.Billing;

namespace Rethink.Services.Common.Models.Claim.History
{
    public class ClaimHistoryFieldSaveModel : ClaimHistorySaveModel
    {
        public ClaimHistoryField ClaimHistoryField { get; set; }
    }
}
