using System;

namespace BillingService.Domain.Models.Reporting
{
    public class ArReportModel
    {
        public string PayerOrFunderName { get; set; }
        public int ClientId { get; set; }
        public string ClientFirstName { get; set; }
        public string ClientLastName { get; set; }
        public DateTime ClaimForm { get; set; }
        public DateTime ClaimThrough { get; set; }
        public string ClaimStatus { get; set; }
        public DateTime? BilledDate { get; set; }
        public Decimal BilledAmount { get; set; }
        public Decimal Adjustment { get; set; }
        public Decimal AdjustedClaimAmount { get; set; }
        public Decimal PaymentRecieved { get; set; }
        public Decimal NetRecievable { get; set; }
        public Decimal OneToThirty { get; set; }
        public Decimal ThirtyOneToSixty { get; set; }
        public Decimal SixtyOneToNinty { get; set; }
        public Decimal NintyOneToOneTwenty { get; set; }
        public Decimal MoreThanOneTwenty { get; set; }
    }
}
