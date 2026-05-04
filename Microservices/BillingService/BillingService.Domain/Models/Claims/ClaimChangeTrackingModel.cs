using Rethink.Services.Common.Enums.Billing;

namespace BillingService.Domain.Models.Claims
{
    public class ClaimChangeTrackingModel
    {
        public ClaimChangeTrackingModel(ClaimAction action, ClaimHistoryAction historyAction, ClaimHistoryField field)
        {
            ClaimAction = action;
            ClaimHistoryAction = historyAction;
            TrackingField = field;
        }

        public ClaimAction ClaimAction { get; set; }
        public ClaimHistoryAction ClaimHistoryAction { get; set; }
        public ClaimHistoryField TrackingField { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }
}
