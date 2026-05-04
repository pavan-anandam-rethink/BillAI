using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Entities.BH.Propagating
{
    public class PropagatingChildProfileAuthorizationEntity : BasePersistEntity, IAuditedEntity
    {
        public string AuthorizationNumber { get; set; }
        [Column("hcChildProfileDiagnosisId")]
        public int ChildProfileDiagnosisId { get; set; }
        [Column("hcAuthorizationRenderingProviderTypeId")]
        public int AuthorizationRenderingProviderTypeId { get; set; }
        public int? RenderingProviderStaffId { get; set; }
        public int NoOfUnits { get; set; }
        public int? SchedulingGoalNoOfUnits { get; set; }
        public int? DiagnosisLUId { get; set; }
        public DateTime? EndDate { get; set; }
        [Column("hcChildProfileAuthorizationId")]
        public int ChildProfileAuthorizationId { get; set; }
        public DateTime? RenderingProviderEndDate { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public virtual MemberEntity ModifiedByMember { get; set; }

    }
}