using System;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class AccountsReceivablesResponse
    {
        public string FunderName { get; set; }
        public int ClientId { get; set; }
        public string ClientFirstName { get; set; }
        public string ClientLastName { get; set; }
        public DateTime ClaimFrom { get; set; }
        public DateTime ClaimThrough { get; set; }
        public string ClaimStatus { get; set; }
        public DateTime? BilledDate { get; set; }
        public decimal BilledAmount { get; set; }
        public decimal Adjustments { get; set; }
        public decimal AdjustedClaimAmount { get; set; }
        public decimal PaymentsReceived { get; set; }
        public decimal NetReceivable { get; set; }
        public decimal OneToThirty { get; set; }
        public decimal ThirtyOneToSixty { get; set; }
        public decimal SixtyOneToNinty { get; set; }
        public decimal NintyOneToOneHundredTwenty { get; set; }
        public decimal MoreThanOneHundredTwenty { get; set; }

    }
}
