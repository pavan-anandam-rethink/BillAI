using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimSubmissionEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("hcClaimId")]
        public int ClaimId { get; set; }
        public string ClaimSubmissionIdentifier { get; set; }

        [Column("documentTypeId")]
        public ClaimDocumentType DocumentType { get; set; }
        [Column("submissionTypeId")]
        public ClaimSubmissionType SubmissionType { get; set; }
        [Column("frequencyTypeId")]
        public ClaimFrequencyType FrequencyType { get; set; }

        [Column("submissionStatusId")]
        public ClaimSubmissionStatus SubmissionStatus { get; set; }

        public string PayerClaimControlNumber { get; set; }

        public string ResponsibilitySequence { get; set; } // P = Primary, S = Secondary, T = Tertiary, 4, 5, 6, 7, 8, 9

        [Column("hcChildProfileAuthorizationId")]
        public int ChildProfileAuthorizationId { get; set; }

        public string PriorClaimSubmissionIdentifier { get; set; }

        [Column("hcPriorClaimSubmissionId")]
        public int PriorClaimSubmissionId { get; set; }

        public string ClaimFilePath { get; set; }
        public string ReportPath { get; set; }

        public DateTime SubmitDate { get; set; }
        public string ErrorMessage { get; set; }

        // collected 837 data
        public string AccountAddress1 { get; set; }
        public string AccountAddress2 { get; set; }
        public string AccountCity { get; set; }
        public string AccountState { get; set; }
        public string AccountZip { get; set; }
        public string AccountCountry { get; set; }
        public string AccountTown { get; set; }
        public string AccountBillingAddress1 { get; set; }
        public string AccountBillingAddress2 { get; set; }
        public string AccountBillingCity { get; set; }
        public string AccountBillingState { get; set; }
        public string AccountBillingZip { get; set; }
        public string AccountBillingCountry { get; set; }
        public string AccountBillingTown { get; set; }
        public string AccountBillingProviderEmail { get; set; }
        public string AccountBillingProviderFax { get; set; }
        public string AccountBillingProviderName { get; set; }
        public string AccountBillingProviderPhone { get; set; }
        public string AccountBillingProviderTaxonomyCode { get; set; }
        public string AccountNpiNumber { get; set; }
        public string AccountFederalTaxId { get; set; }
        public string AccountPhoneNumber { get; set; }
        public string AuthorizationNumber { get; set; }
        public string AuthorizedPaymentConfirmationType { get; set; }
        public string ChildProfileAddress1 { get; set; }
        public string ChildProfileAddress2 { get; set; }
        public string ChildProfileCity { get; set; }
        public string ChildProfileState { get; set; }
        public string ChildProfileZip { get; set; }
        public string ChildProfileCountry { get; set; }
        public string ChildProfileTown { get; set; }
        public string ChildProfileFirstName { get; set; }
        public string ChildProfileLastName { get; set; }
        public string ChildProfileMiddleName { get; set; }
        public DateTime? ChildProfileDOB { get; set; }
        public string ChildProfileGender { get; set; }
        public string ClearinghouseIdentifier { get; set; }
        public string ClearinghouseProviderIdentifier { get; set; }
        public string ClearinghouseSubmitterName { get; set; }
        public BillingProviderOptionType? FunderBillingProviderOption { get; set; } //TODO: check about serviceLineBillingProviderOption usage in EdiGenerator 

        [Column("hcFunderId")]
        public int? FunderId { get; set; }

        public string LocationBillingProviderAddress1 { get; set; }
        public string LocationBillingProviderAddress2 { get; set; }
        public string LocationBillingProviderCity { get; set; }
        public string LocationBillingProviderState { get; set; }
        public string LocationBillingProviderZip { get; set; }
        public string LocationBillingProviderCountry { get; set; }
        public string LocationBillingProviderTown { get; set; }
        public string LocationBillingProviderFederalTaxId { get; set; }
        public bool? LocationBillingProviderIsBillingLocation { get; set; }
        public string LocationBillingProviderName { get; set; }
        public string LocationBillingProviderNpiNumber { get; set; }
        public string LocationBillingProviderTaxonomyCode { get; set; }
        public string LocationBillingProviderCommercialNumber { get; set; }
        public string LocationBillingProviderStateLicenseNumber { get; set; }
        public string LocationBillingProviderLocationNumber { get; set; }
        public string PlaceOfServiceCode { get; set; }
        public string ReferringProviderFirstName { get; set; }
        public string ReferringProviderLastName { get; set; }
        public string ReferringProviderNpiNumber { get; set; }
        public string ReleaseOfInformationConfirmationType { get; set; }
        public string RenderingProviderStaffFirstName { get; set; }
        public string RenderingProviderStaffLastName { get; set; }
        public string RenderingProviderStaffMiddleName { get; set; }
        public string RenderingProviderStaffNpiNumber { get; set; }
        public string RenderingProviderStaffTaxonomyCode { get; set; }
        public string ServiceLocationAddress1 { get; set; }
        public string ServiceLocationAddress2 { get; set; }
        public string ServiceLocationCity { get; set; }
        public string ServiceLocationState { get; set; }
        public string ServiceLocationZip { get; set; }
        public string ServiceLocationCountry { get; set; }
        public string ServiceLocationName { get; set; }
        public string ServiceLocationNpiNumber { get; set; }

        public decimal? TotalPatientPaid { get; set; }


        // resolved values
        public string ResolvedBillingProviderNpi { get; set; }
        public string ResolvedBillingProviderFederalTaxID { get; set; }
        public string ResolvedBillingProviderName { get; set; }
        public string ResolvedBillingProviderFirstName { get; set; }
        public string ResolvedBillingProviderMiddleName { get; set; }

        public string ResolvedRenderingProviderNpi { get; set; }
        public string ResolvedRenderingProviderName { get; set; }
        public string ResolvedRenderingProviderFirstName { get; set; }
        public string ResolvedRenderingProviderMiddleName { get; set; }



        // Calculated value - set by fetch data and used later in EDI processing
        public PaymentClaimEntity PriorFunderLatestClaimPayment { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }


        public virtual ClaimEntity Claim { get; set; }
        public virtual ClaimSubmissionEntity PriorClaimSubmission { get; set; }
        public virtual ClaimSubmissionEntity NextClaimSubmission { get; set; } // populated via EF one-to-one when it populates PriorClaimSubmission 
        [NotMapped]
        public virtual FunderDataModel FunderDetails { get; set; }
        public virtual ClientAuthorization ChildProfileAuthorization { get; set; }
        public ICollection<ClaimValidationErrorEntity> ClaimValidationErrors { get; set; }
        public ICollection<ClaimSubmissionServiceLineEntity> ClaimSubmissionServiceLines { get; set; }
        public ICollection<ClaimSubmissionFunderSequenceEntity> ClaimSubmissionFunderSequences { get; set; }
    }
}