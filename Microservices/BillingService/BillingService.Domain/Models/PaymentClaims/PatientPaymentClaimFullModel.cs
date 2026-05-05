using BillingService.Domain.DTO;
using Rethink.Services.Common.Entities.Billing.Payment;
using System;
using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class PatientPaymentClaimFullModel
    {
        public int PaymentId { get; set; }
        public int PatientId { get; set; }
        public int ClaimId { get; set; }
        public int ChargeId { get; set; }
        public string ServiceCode { get; set; }
        public DateTime DateOfService { get; set; }
        public string PatientName { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? InsurancePayment { get; set; }
        public decimal? PatientPayment { get; set; }
        public decimal? totalAmount { get; set; }
        public decimal? PositiveAdjustment { get; set; }
        public decimal? NegativeAdjustment { get; set; }
        public decimal? PositivePatientResponsibility { get; set; }
        public decimal? NegativePatientResponsibility { get; set; }
        public decimal? Adjustment { get; set; }
        public decimal? PatientResponsibility { get; set; }
        public decimal? PatientResponsibilityBalance { get; set; }
        public decimal? Balance { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public int TotalCount { get; set; }
        public List<PaymentClaimServiceLineAdjustmentEntity> Adjustments { get; set; }
        public int PaymentClaimId { get; set; } = 0;
        public decimal? UnallocatedPayment { get; set; }
    }

    public class PaymentGroupedModel
    {
        public int PaymentId { get; set; }
        public int PaymentTypeId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int PaymentClaimId { get; set; }
        public int ClaimId { get; set; }
        public int ServiceLineId { get; set; }
        public int ChargeId { get; set; }
        public DateTime? DateOfService { get; set; }
        public string ServiceCode { get; set; }
        public decimal? ChargeAmount { get; set; }
        public decimal? AllowedAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public string ProcedureModifier1 { get; set; }
        public string ProcedureModifier2 { get; set; }
        public string ProcedureModifier3 { get; set; }
        public string ProcedureModifier4 { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public List<PaymentClaimServiceLineAdjustmentEntity> Adjustments { get; set; }
        public List<AdjustmentDto> Adjustment { get; set; } = new List<AdjustmentDto>();

        public bool HasErrors { get; set; }

    }
}
