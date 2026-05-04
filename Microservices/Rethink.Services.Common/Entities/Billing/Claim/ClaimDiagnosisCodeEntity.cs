using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimDiagnosisCodeEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("hcClaimId")]
        public int ClaimId { get; set; }
        public int DiagnosisId { get; set; }
        public int Order { get; set; }
        public bool IncludeOnClaims { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ClaimEntity Claim { get; set; }
        public virtual DiagnosisEntityModel Diagnosis { get; set; }
    }
}