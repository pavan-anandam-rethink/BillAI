using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Models.PaymentClaimServiceLine;
using BillingService.Domain.Models.PaymentClaimServiceLineAdjustment;
using Rethink.Services.Common.Entities.Billing.Payment;

namespace BillingService.Domain.Models.BulkPaymentPosting
{
    [ExcludeFromCodeCoverage]
    public class BulkPaymentResponseModel : UserInfo
    {
        public int Id { get; set; }
        public int? ClaimId { get; set; }
        public string ClaimIdentifier { get; set; }
        public int? ChargeEntryId { get; set; }
        public decimal? Units { get; set; }
        public List<string> ReasonCode { get; set; }
        public List<string> Description { get; set; }
        public List<ReasonCodeData> ReasonCodeData { get; set; }
        public bool IsLinked { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? PatientResponsibility { get; set; }
        public decimal? PatientResponsibilityBalance { get; set; }
        public decimal? Adjustment { get; set; }
        public string Status { get; set; }
        public decimal? InsurancePayment { get; set; }
        public int ServiceLineId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public DateTime DateOfService{ get; set; }
        public string Procedure { get; set; }
        public string Mods { get; set; }
        public decimal BilledAmount { get; set; }
        public decimal AllowedAmount { get; set; }
        public decimal? PatientPayment { get; set; }
        public decimal? PositiveAdjustment { get; set; }
        public decimal? NegativeAdjustment { get; set; }
        public decimal? PositivePatientResponsibility { get; set; }
        public decimal? NegativePatientResponsibility { get; set; }
        public List<PaymentClaimServiceLineAdjustmentModel> Adjustments { get; set; }
        public decimal? Balance { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public int TotalCount { get; set; }
        public bool HasErrors { get; set; }
        public decimal WriteOff { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
    }

}
