using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Enums.BH;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimSubmissionFunderSequenceEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("hcClaimSubmissionId")]
        public int ClaimSubmissionId { get; set; }

        [Column("hcFunderId")]
        public int FunderId { get; set; }

        public string FunderResponsibilitySequence { get; set; }
        public int SequenceOrder { get; set; }
        public string FunderName { get; set; }
        public string FunderVendorId { get; set; }
        public string InsurancePolicyNumber { get; set; }
        public string InsuranceGroupName { get; set; }
        public string InsurancePlanName { get; set; }
        public string InsuranceGroupNumber { get; set; }
        public string InsuranceCoverageType { get; set; }// see Loop 2320.SBR09 for values
        public string InsuranceAddress1 { get; set; }
        public string InsuranceAddress2 { get; set; }
        public string InsuranceCity { get; set; }
        public string InsuranceState { get; set; }
        public string InsuranceZip { get; set; }
        public string InsuranceCountry { get; set; }
        public string InsuranceTown { get; set; }
        public string SubscriberFirstName { get; set; }
        public string SubscriberLastName { get; set; }
        public string SubscriberMiddleName { get; set; }
        public DateTime? SubscriberDOB { get; set; }
        public string SubscriberGender { get; set; }
        public string SubscriberAddress1 { get; set; }
        public string SubscriberAddress2 { get; set; }
        public string SubscriberCity { get; set; }
        public string SubscriberState { get; set; }
        public string SubscriberZip { get; set; }
        public string SubscriberCountry { get; set; }
        public string SubscriberTown { get; set; }
        public int? RelationshipToSubscriber { get; set; } // 1 = self, 2 = dependent/child
        public DateTime? ReleaseOfInformationConfirmationDate { get; set; }
        public string MedicalRecordNumber { get; set; }
        public BillingProviderOptionType? ServiceLineBillingProviderOption { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ClaimSubmissionEntity ClaimSubmission { get; set; }
    }
}