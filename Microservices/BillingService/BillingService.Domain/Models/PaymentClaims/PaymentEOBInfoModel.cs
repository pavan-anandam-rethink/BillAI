using System;
using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentClaims
{
    public class ClaimEOBInfoModel
    {
        public int Id { get; set; }
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public string ClaimIdentifier { get; set; }
        public decimal? BilledAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? PatientResponsibility { get; set; }
        public decimal? AllowedAmount { get; set; }
        public string Status { get; set; }
        public List<PaymentClaimServiceLineModel> ServiceLines { get; set; }
        public string ProviderName { get; set; }
        public string ProviderId { get; set; }
        public string PayerClaimNumber { get; set; }
        public string PlaceOfService { get; set; }
        public string POSCode { get; set; }
        public int ClaimId { get; set; }
        public int AccountInfoId { get; set; }
        public int RenderingProviderTypeId { get; set; }
        public DateTime? ClaimDateFrom { get; set; }
        public DateTime? ClaimDateTo { get; set; }
    }
}