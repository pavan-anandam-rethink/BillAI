using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class UpdateClaimDetailsModel
    {
        public int AccountInfoId { get; set; }
        public int MemberId { get; set; }

        public int ClaimId { get; set; }
        public List<ClaimDiagnosisCodeUpdateModel> DiagnosisCodes { get; set; }

        public int PlaceOfServiceId { get; set; }
        public string PlaceOfService { get; set; }

        public int RenderingProviderId { get; set; }
        public string RenderingProvider { get; set; }
        public int? BillingProviderId { get; set; }
        public string BillingProvider { get; set; }
        public int? ReferringProviderId { get; set; }
        public string ReferringProvider { get; set; }
        public int? ServiceFacilityId { get; set; }
        public string ServiceFacility { get; set; }

        public int SubmissionReasonId { get; set; }
        public int? PatientReleaseAgreementId { get; set; }
        public int? AuthorizePaymentId { get; set; }
        public int BenefitAssignmentId { get; set; }
        public string OriginalClaim { get; set; }
        public string Note { get; set; }
    }

    public class UpdateDetails : UserInfo
    {
        public bool isClaimUpdated { get; set; }
        public bool isChargeEntryUpdated { get; set; }
        public UpdateClaimDetailsModel claimModel { get; set; }
        public UpdateBillingClaimDetailsListModel chargeEntryModel { get; set; }
        public string? ImpersonationUserName { get; set; }
        public BillingProviderRequest? BillingProviderRequest { get; set; }


    }

    public class BillingProviderRequest
    {
        public int ClaimId { get; set; }
        public BillingProvider BillingProvider { get; set; }
    }

    public class BillingProvider
    {
        public string ProviderType { get; set; }
        public string FirstName { get; set; }
        public string LastNameOrFacilityName { get; set; }
        public string Npi { get; set; }
        public string TaxId { get; set; }
        public string TaxonomyCode { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string ZipExt { get; set; }
    }
}
