using BillingService.Domain.Models.PaymentPosting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class ClaimSaveModel
    {
        public ClaimInfo ClaimInfo { get; set; }
        public Provider Provider { get; set; }
        public DiagnosisCode DiagnosisCode { get; set; }
        public string? ImpersonationUserName { get; set; }
    }

    public class ClaimInfo
    {
        public int ClientId { get; set; }
        public int ClientFunderId { get; set; }
        public int FunderId { get; set; }
        public int ResponsiblePartyId { get; set; }
        public int ServiceLineId { get; set; }
        public int ServiceId { get; set; }
        public int? AuthorizationNumberId { get; set; }
        public string AuthorizationNumber { get; set; }
        public bool AllowManualAuthorization { get; set; }
        public int PlaceOfServiceCodeId { get; set; }
    }

    public class Provider
    {
        public int RenderingProviderId { get; set; }
        public int RenderingProviderTypeId { get; set; }
        public int? BillingProviderId { get; set; }
        public int? ServiceFacilityLocationId { get; set; }
        public DateTime DateOfServiceStart { get; set; }
        public DateTime DateOfServiceEnd { get; set; }
        public int? ReferringProviderId { get; set; }
    }

    public class DiagnosisCode
    {
        public List<ClaimDiagnosisCodeModel> DiagnosisCodesToSave { get; set; }
        public List<ClaimBillingCodeModel> BillingCodes { get; set; }
    }


    public class ClaimBillingCodeModel
    {
        public int BillingCodeId { get; set; }
        public int UnitTypeId { get; set; }
        public DateTime IndividualDateOfService { get; set; }
        public string Modifier1 { get; set; }
        public string Modifier2 { get; set; }
        public string Modifier3 { get; set; }
        public string Modifier4 { get; set; }
        public decimal NoOfUnits { get; set; }
        public string Note { get; set; }
        public decimal Rate { get; set; }
        public decimal TotalCharges { get; set; }

        public int RenderingProviderStaffId { get; set; }
        public bool IsSecondaryCode { get; set; }
    }
}
