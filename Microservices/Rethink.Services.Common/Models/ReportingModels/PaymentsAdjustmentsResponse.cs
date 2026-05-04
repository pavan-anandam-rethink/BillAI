using System;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class PaymentsAdjustmentsResponse
    {
        public string FunderName { get; set; }
        public int? ClientId { get; set; }
        public string ClientLast { get; set; }
        public string ClientFirst { get; set; }
        public DateTime ClaimFrom { get; set; }
        public DateTime ClaimThrough { get; set; }
        public string ClaimStatus { get; set; }
        public DateTime? BilledDate { get; set; }
        public string? TransactionType { get; set; }
        public string? ReasonCode { get; set; }
        public string? RemarkCode { get; set; }
        public DateTime? TransactionDate { get; set; }
        public DateTime? PaymentOrAdjustmentDate { get; set; }
        public string? EftOrCheckNumber { get; set; }
        public decimal? Payment { get; set; }
        public decimal Adjustment { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
    }
}
