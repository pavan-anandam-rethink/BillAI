using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.BH;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class ClaimHFCAModel
    {
        public string AuthorizationNumber { get; internal set; }
        public int FunderId { get; internal set; }
        public string PatientName { get; internal set; }
        public string FunderAddress { get; internal set; }
        public string FunderName { get; internal set; }
        public int? InsuredCoverageTypeId { get; internal set; }
        public string InsuredNumber { get; internal set; }
        public DateTime PatientDOB { get; internal set; }
        public int? PatientSex { get; internal set; }
        public string PatientAddress { get; internal set; }
        public string PatientAddress2 { get; internal set; }
        public string PatientCity { get; internal set; }
        public string PatientState { get; internal set; }
        public int? PatientRelationShipToInsured { get; internal set; }
        public string FunderAddress2 { get; internal set; }
        public string FunderCity { get; internal set; }
        public string FunderState { get; internal set; }
        public string FunderZip { get; internal set; }
        public string PatientZip { get; internal set; }
        public string FunderMobile { get; internal set; }
        public string PatientMobile { get; internal set; }
        public string InsuredAddress { get; internal set; }
        public string InsuredAddress2 { get; internal set; }
        public string InsuredCity { get; internal set; }
        public string InsuredZip { get; internal set; }
        public string InsuredState { get; internal set; }
        public string InsuredMobile { get; internal set; }
        public string InsuredName { get; internal set; }
        public string InsuredPolicyGroupNumber { get; internal set; }
        public int? InsuredSex { get; internal set; }
        public DateTime? InsuredDOB { get; internal set; }
        public int IsAnotherPlan { get; internal set; }
        public string SecondaryInsuredName { get; internal set; }
        public string SecondaryInsuredNumber { get; internal set; }
        public string SecondaryInsurancePlanName { get; internal set; }
        public int Id { get; internal set; }
        public string ReleaseOfInformationConfirmationType { get; internal set; }
        public int? AuthoriseReleaseOfInfo { get; internal set; }
        public string AuthorizedPaymentConfirmationType { get; internal set; }
        public DateTime? PatientFunderSignatureDate { get; internal set; }
        public List<string> PatientDiagnosis { get; internal set; }
        public DateTime AuthorizationStartDate { get; internal set; }
        public DateTime AuthorizationEndDate { get; internal set; }
        public string LocationNumber { get; internal set; }
        public IList<ClaimChargeEntryEntity> ClaimChargeEntries { get; internal set; }
        public string FederalTaxId { get; internal set; }
        public string MedicalRecordNumber { get; internal set; }
        public decimal TotalCharge { get; internal set; }
        public decimal Paid { get; internal set; }
        public string RenderingProviderName { get; internal set; }
        public string ProviderLocation { get; internal set; }
        public string ProviderAddress1 { get; internal set; }
        public string ProviderAddress2 { get; internal set; }
        public string ProviderCity { get; internal set; }
        public string ProviderCountry { get; internal set; }
        public string ProviderZip { get; internal set; }
        public string ProviderName { get; internal set; }
        public string ProviderState { get; internal set; }
        public string ProviderLocationNPI { get; internal set; }
        public string ProviderLocationTaxId { get; internal set; }
        public string ServiceLocation { get; internal set; }
        public string ServiceAddress1 { get; internal set; }
        public string ServiceAddress2 { get; internal set; }
        public string ServiceCity { get; internal set; }
        public string ServiceCountry { get; internal set; }
        public string ServiceZip { get; internal set; }
        public string ServiceName { get; internal set; }
        public string ServiceState { get; internal set; }
        public string ServiceLocationNPI { get; internal set; }
        public string ServiceLocationTaxId { get; internal set; }
        public string ServicePhoneNumber { get; internal set; }
        public string AccountName { get; internal set; }
        public string AccountAddress1 { get; internal set; }
        public string AccountCity { get; internal set; }
        public string AccountState { get; internal set; }
        public string AccountZip { get; internal set; }
        public string AccountPhone { get; internal set; }
        public string AccountNPI { get; internal set; }
        public string ReferringProviderName { get; internal set; }
        public string ReferringProviderNPI { get; internal set; }
        public string InsurancePlanName { get; internal set; }
        public string ProviderPhoneNumber { get; internal set; }
        public string ProviderTaxonomyCode { get; internal set; }
        public string ProviderZipExt { get; internal set; }
        public BillingProviderOptionType? ServiceLineBillingProviderOption { get; internal set; }
    }
}