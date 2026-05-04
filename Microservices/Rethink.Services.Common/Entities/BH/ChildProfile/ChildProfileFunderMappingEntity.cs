using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Billing;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Entities.BH.ChildProfile
{
    public class ChildProfileFunderMappingEntity : BasePersistEntity, IAuditedEntity
    {
        public int ChildProfileId { get; set; }
        [Column("hcFunderId")]
        public int FunderId { get; set; }
        [Column("hcFunderCaseManagerId")]
        public int? FunderCaseManagerId { get; set; }
        [Column("hcChildProfileInsuranceContact")]
        public int? ChildProfileInsuranceContact { get; set; }
        public string ClientFunderMRNNo { get; set; }
        public string CaseNumber { get; set; }
        public string ServiceContractDetails { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? ReleaseOfInformationConfirmationTypeId { get; set; }
        public DateTime? ReleaseOfInformationConfirmationDate { get; set; }
        public int? AuthorizedPaymentConfirmationTypeId { get; set; }
        public bool? IsAutismCoveredBenefit { get; set; }
        
        public string CaseNumber2 { get; set; }
        public int? KareoInsurancePolicyId { get; set; }
        [Column("hcFunderInsurancePlanId")]
        public int? FunderInsurancePlanId { get; set; }
        public int? KareoCaseNumber { get; set; }
        //[Column("hcFunderCoverageTypeId")]
        //public int? FunderCoverageTypeId { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual FunderEntity Funder { get; set; }
        public virtual ChildProfileEntity ChildProfile { get; set; }
        public List<ChildProfileFunderServiceLineMappingEntity> ChildProfileFunderServiceLineMapings { get; set; }
        public virtual ChildProfileContactEntity InsuranceContact { get; set; }
        //public virtual FunderCaseManagerEntity FunderCaseManager { get; set; }
        public IEnumerable<ChildProfileFunderMappingNoteEntity> ChildProfileFunderMappingNotes { get; set; }

    }
}