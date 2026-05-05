using System;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class PaymentClaimModel : PatientPaymentClaimFullModel
    {
        public int Id { get; set; }
        public int? ClaimId { get; set; }
        public string ClaimIdentifier { get; set; }
        public string ClaimStatus { get; set; } = string.Empty;
        public decimal? BilledAmount { get; set; }
        public decimal? AllowedAmount { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public decimal? PatientResponsibility { get; set; }
        public int CmsPageCount { get; set; }
        public string Status { get; set; }
        public DateTime DateOfServiceStart { get; set; }
        public bool IsFlagged { get; set; }
        public string ClaimActionTypes { get; set; } = string.Empty;
        public bool? isSecondaryPayerAvailable { get; set; } = null;
        public int? submissionTypeId { get; set; } = null;
        public bool? isTestAccount { get; set; } = null;
    }
}