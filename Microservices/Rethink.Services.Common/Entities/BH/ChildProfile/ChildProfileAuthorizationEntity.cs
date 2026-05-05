using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Billing;
using Rethink.Services.Common.Entities.BH.Member;
using Rethink.Services.Common.Entities.BH.Service;

namespace Rethink.Services.Common.Entities.BH.ChildProfile
{
    public class ChildProfileAuthorizationEntity : BasePersistEntity, IAuditedEntity
    {
        public int ChildProfileId { get; set; }
        [Column("hcFunderId")]
        public int FunderId { get; set; }
        [Column("hcProviderServiceId")]
        public int ProviderServiceId { get; set; }
        [Column("hcAuthorizationTypeId")]
        public int? AuthorizationTypeId { get; set; }
        public string AuthorizationNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public byte[] Attachment { get; set; }
        public string AttachmentFileName { get; set; }
        [Column("hcAuthorizationDistributionTypeId")]
        public int AuthorizationDistributionTypeId { get; set; }
        [Column("hcChildProfileDiagnosisId")]
        public int? ChildProfileDiagnosisId { get; set; }
        [Column("hcAuthorizationRenderingProviderTypeId")]
        public int? AuthorizationRenderingProviderTypeId { get; set; }
        public int? RenderingProviderStaffId { get; set; }
        [Column("hcAuthorizationSubmissionTypeId")]
        public int? AuthorizationSubmissionTypeId { get; set; }
        public int? TotalNumberofUnits { get; set; }
        
        [Column("hcChildProfileFunderServiceLineMappingId")]
        public int? ChildProfileFunderServiceLineMappingId { get; set; }
        [Column("hcServiceFacilityLocationId")]
        public int? ServiceFacilityLocationId { get; set; }
        [Column("hcBillingProviderId")]
        public int? BillingProviderId { get; set; }   
        [Column("hcChildProfileReferringProviderId")]
        public int? ChildProfileReferringProviderId { get; set; }
        public DateTime? InactiveDate { get; set; }


        public DateTime? RenderingProviderDateUpdated { get; set; }
        public DateTime? ReferringProviderDateUpdated { get; set; }
        public DateTime? BillingProviderDateUpdated { get; set; }
        public DateTime? ServiceFacilityLocationDateUpdated { get; set; }
        public int? ShowAuthorizationByTypeId { get; set; }
        public int? DiactivatedById { get; set; }


        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ChildProfileFunderServiceLineMappingEntity ChildProfileFunderServiceLineMapping { get; set; }
        public virtual ChildProfileDiagnosisEntity ChildProfileDiagnosis { get; set; }
        public virtual ChildProfileEntity ChildProfile { get; set; }
        public virtual IEnumerable<ChildProfileAuthorizationBillingCodeEntity> ChildProfileAuthorizationBillingCodes { get; set; }
        public virtual IEnumerable<ChildProfileAuthorizationDiagnosisCodeEntity> ChildProfileAuthorizationDiagnosisCodes { get; set; }
        public virtual ProviderServiceLineEntity ProviderServiceLine { get; set; }
        public virtual ProviderLocationEntity ServiceFacilityLocation { get; set; }
        public virtual ProviderLocationEntity BillingProvider { get; set; }
        public virtual ChildProfileReferringProviderEntity ChildProfileReferringProvider { get; set; }
        public virtual FunderEntity Funder { get; set; }
        public virtual MemberEntity RenderingProvider { get; set; }
    }
}