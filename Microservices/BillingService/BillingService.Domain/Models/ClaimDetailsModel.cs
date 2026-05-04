using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using System;
using System.Collections.Generic;

namespace BillingService.Domain.Models
{
    public class ClaimDetailsModel
    {
        public int Id { get; set; }
        public string ClaimIdentifier { get; set; }
        public int? PatientId { get; set; }
        public string PatientName { get; set; }
        public string ResponsibleParty { get; set; }
        public DateTime DateOfServiceStart { get; set; }
        public DateTime DateOfServiceEnd { get; set; }
        public List<ClaimDiagnosisCodeModel> DiagnosisCodes { get; set; }
        public string AuthorizationNumber { get; set; }
        public string? AuthorizationStatus { get; set; } = "Yes";
        public string RenderingProvider { get; set; }
        public string ReferringProvider { get; set; }
        public string BillingProvider { get; set; }
        public string ServiceFacility { get; set; }
        public int? RenderingProviderId { get; set; }
        public int? ReferringProviderId { get; set; }
        public bool ReferringProviderRequiredOnClaim { get; set; }
        public int? BillingProviderId { get; set; }
        public int? ServiceFacilityId { get; set; }

        public decimal BilledAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal PatientResponsibilityAmount { get; set; }

        public string PlaceOfService { get; set; }
        public int PlaceOfServiceId { get; set; }
        public int? PatientReleaseAgreement { get; set; }
        public int? AuthorizePayment { get; set; }
        public int? BenefitsAssignment { get; set; }
        public string ProviderSignature { get; set; }
        public int SubmissionReason { get; set; }
        public string SubmissionCode { get; set; }
        public string OriginalClaim { get; set; }
        public string Note { get; set; }
        public ClaimStatus ClaimStatus { get; set; }
        public int? FunderId { get; set; }
        public string FunderName { get; set; }
        public FunderType? FunderTypeId { get; set; }
        public BillingProviderOptionType? BillingProviderOptionId { get; set; }

        public AuthorizationDetailsModel AuthorizationDetails { get; set; }
        public int? ServiceLineId { get; set; }

        public string? ServiceLine { get; set; }
        public int ServiceId { get; set; }
        public int PrimaryFunderId { get; set; }
        public int? SecondaryFunderId { get; set; }
    }


    public class AuthorizationDetailsModel
    {
        public int? RenderingProviderId { get; set; }
    }
}