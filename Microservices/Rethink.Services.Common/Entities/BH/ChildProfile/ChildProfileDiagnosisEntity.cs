using System;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Company;

namespace Rethink.Services.Common.Entities.BH.ChildProfile
{
    public class ChildProfileDiagnosisEntity : BasePersistEntity, IAuditedEntity
    {
        public int ChildProfileId { get; set; }
        public int? DiagnosisId { get; set; }
        public int Order { get; set; }
        public int ServiceLineId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }


        public virtual DiagnosisEntity Diagnosis { get; set; }
    }
}