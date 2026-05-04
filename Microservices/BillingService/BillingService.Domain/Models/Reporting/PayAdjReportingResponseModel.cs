using System;

namespace BillingService.Domain.Models.Reporting
{
    public class PayAdjReportingResponseModel
    {
        public string FunderName { get; set; }
        public int ClientId { get; set; }
        public string ClientLast { get; set; }
        public string ClientFirst { get; set; }
        public DateTime ClaimFrom { get; set; }
        public DateTime ClaimThrough { get; set; }
        public string ClaimStatus { get; set; }
        public DateTime? BilledDate { get; set; }
        public int? TransactionType { get; set; }
        public string? ReasonCode { get; set; }
        public int? RemarkCode { get; set; }
        public DateTime? TransactionDate { get; set; }
        public DateTime? PaymentOrAdjustmentDate { get; set; }
        public string? EftOrCheckNumber { get; set; }
        public decimal? Payment { get; set; }
        public decimal? Adjustment { get; set; }

    }
}
