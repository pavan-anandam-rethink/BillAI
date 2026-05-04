using Rethink.Services.Common.Entities.Billing.Payment;
using System;
using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class PaymentClaimServiceLineModel
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int PaymentId { get; set; }
        public int PaymentTypeId { get; set; }
        public int? ChargeEntryId { get; set; }
        public DateTime? DateOfService { get; set; }
        public string Procedure { get; set; }
        public string Mods { get; set; }
        public decimal? BilledAmount { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public decimal? AllowedAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? InsurancePayment { get; set; }
        public decimal? PatientPayment { get; set; }
        public decimal? ServiceLinePaymentAmount { get; set; }
        public decimal? Adjustment { get; set; }
        public decimal? PatientResponsibility { get; set; }
        public decimal? PatientResponsibilityBalance { get; set; }
        public decimal? Balance { get; set; }
        public DateTime? DateLastModified { get; set; }
        public int ClaimId { get; set; }
        public string ClaimIdentifier { get; set; }
        public bool HasErrors { get; set; }
        public bool IsLinked { get; set; }
        public decimal? Units { get; set; }
        public List<string> ReasonCode { get; set; }
        public List<string> GroupCode { get; set; }
        public List<string> CombinedCode { get; set; }
        public List<string> Description { get; set; }
        public List<ReasonCodeData> ReasonCodeData { get; set; }
        public decimal? DeductibleAmount { get; set; }
        public decimal? CoPayCoInsAmount { get; set; }
        public List<PaymentClaimServiceLineAdjustmentEntity> Adjustments { get; set; }
        public decimal UnallocatedPayment { get; set; } = 0;
    }

    public class ReasonCodeData
    {
        public string ReasonCode { get; set; }
        public string CombinedCode { get; set; }
        public string Description { get; set; }
    }
}