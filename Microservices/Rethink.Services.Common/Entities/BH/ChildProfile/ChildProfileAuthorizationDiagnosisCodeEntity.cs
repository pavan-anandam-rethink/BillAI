using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Company;

namespace Rethink.Services.Common.Entities.BH.ChildProfile
{
    public class ChildProfileAuthorizationDiagnosisCodeEntity : BasePersistEntity, IAuditedEntity
    {        
        [Column("hcChildProfileAuthorizationId")]
        public int ChildProfileAuthorizationId { get; set; }
        [Column("hcChildProfileDiagnosisId")]
        public int ChildProfileDiagnosisId { get; set; }
        public int DiagnosisId { get; set; }
        public int Order { get; set; }
        public bool IncludeOnClaims { get; set; }
        
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        
        public virtual ChildProfileAuthorizationEntity ChildProfileAuthorization { get; set; }
        public virtual DiagnosisEntity Diagnosis { get; set; }
        public virtual ChildProfileDiagnosisEntity ChildProfileDiagnosis { get; set; }

    }
}