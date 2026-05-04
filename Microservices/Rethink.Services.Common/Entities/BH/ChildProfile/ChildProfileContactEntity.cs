using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Company;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Entities.BH.ChildProfile
{
    public class ChildProfileContactEntity : BasePersistEntity, IAuditedEntity
    {
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


        public virtual MemberEntity Member { get; set; }
        public virtual PersonEntity Person { get; set; }
        public virtual AddressEntity Address { get; set; }
        public virtual ChildProfileEntity ChildProfile { get; set; }
        public List<ChildProfileFunderMappingEntity> ChildProfileFunderMapping { get; set; }
    }
}