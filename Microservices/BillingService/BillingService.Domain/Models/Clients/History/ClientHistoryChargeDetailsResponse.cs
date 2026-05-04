using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Enums.Billing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Models.Clients.History
{
    public class ClientHistoryChargeDetailsResponse
    {
        public List<ClientHistoryChargeDetails> ChargeDetails { get; set; }
        public int Total { get; set; }
    }
    public class ClientHistoryChargeDetails
    {
        public DateTime DateOfService { get; set; }
        public string BillingCode { get; set; }
        public string PlaceOfService { get; set; }
        public string RenderingProvider { get; set; }
        public string AuthorizationNumber { get; set; }
        public string Modifiers { get; set; }
        public string Diagnosis { get; set; }
        public string? PrimaryFunder { get; set; }
        public string PrimaryClaimID { get; set; }
        public string ClaimStatus { get; set; }
        public double Hours { get; set; }
        public decimal Units { get; set; }
        public decimal? PerUnitCharge { get; set; }
        public decimal? BilledAmount { get; set; }
        public decimal? InsurancePayment { get; set; }
        public decimal? Adjustments { get; set; }
        public decimal PatientResponsibilityAdjustments { get; set; }
        public decimal ClaimBalance { get; set; }
        public string? InvoiceNumber { get; set; }
        public string InvoiceStatus { get; set; }
        public decimal? PatientResponsibility { get; set; }
        public decimal? PatientPayments { get; set; }
        public decimal? PatientBalance { get; set; }
        public int? ChargeId { get; set; }
    }

    public class ClientHistoryChargeDetailsModel
    {
        public DateTime DateOfService { get; set; }
        public string? BillingCode { get; set; }
        public string? PlaceOfService { get; set; }
        public int? PlaceOfServiceId { get; set; }
        public string? RenderingProvider { get; set; }
        public string? AuthorizationNumber { get; set; }
        public int? AuthorizationNumberId { get; set; }
        public string? Modifiers { get; set; }
        public string? Diagnosis { get; set; }
        public int PrimaryFunderId { get; set; }
        public string? PrimaryFunder { get; set; }
        public string? PrimaryClaimID { get; set; }
        public string? ClaimStatus { get; set; }
        public int? UnitTypeId { get; set; }
        public int? UnitTypeValue { get; set; }
        public decimal Units { get; set; }
        public decimal? PerUnitCharge { get; set; }
        public decimal? BilledAmount { get; set; }
        public decimal? InsurancePayment { get; set; }
        public decimal? Adjustments { get; set; }
        public decimal? PatientResponsibilityAdjustments { get; set; }
        public decimal? ClaimBalance { get; set; }
        public string? InvoiceNumber { get; set; }
        public decimal? PatientResponsibility { get; set; }
        public decimal? PatientPayments { get; set; }
        public decimal? PatientBalance { get; set; }
        public int? PaymentClaimServiceLineId {  get; set; }
        public int? RenderingProviderId { get; set; }
        public string InvoiceStatus { get; set; }
        public decimal? WriteOffs { get; set; }
        public int? ChargeId { get; set; }
        public int ClaimChargeId { get; set; }
    }

}
