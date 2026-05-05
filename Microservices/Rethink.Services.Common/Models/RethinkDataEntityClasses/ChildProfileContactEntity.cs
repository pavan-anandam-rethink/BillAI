using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common
{
    public class ChildProfileContactEntityModel
    {
        public int Id { get; set; }
        public int ChildProfileId { get; set; }
        public string ContactRelationship { get; set; }
        public string EncryptedInsuranceNo { get; set; }
        public int? MemberId { get; set; }
        public int PersonId { get; set; }
        public int AddressId { get; set; }
        public string VerificationCode { get; set; }
        public int? RelationshipToInsured { get; set; }
        public int? InsuranceType { get; set; }
        public int? PayorType { get; set; }
        public DateTime? DOB { get; set; }
        public int? GenderId { get; set; }
        public int? MaritalStatusId { get; set; }
        public string InsurancePolicyNumber { get; set; }
        public string InsuranceGroupNumber { get; set; }
        public string Employer { get; set; }
        public decimal? IndividualDeductible { get; set; }
        public decimal? FamilyDeductible { get; set; }
        public decimal? IndividualMOOP { get; set; }
        public decimal? FamilyMOOP { get; set; }
        public decimal? Copay { get; set; }
        public int? Coinsurance { get; set; }
        public int? CreditCardTypeId { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public int? CvvNumber { get; set; }
        public bool? AuthorizationToCharge { get; set; }
        public bool? HasAggressiveBehavior { get; set; }
        [Column("hcTimezoneId")]
        public int? TimezoneId { get; set; }
        public string MedicalRecordNumber { get; set; }
        [Column("hcCopaymentTypeId")]
        public int? CopaymentTypeId { get; set; }


        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

    }
}