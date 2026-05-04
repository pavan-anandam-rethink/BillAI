using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.Billing
{
    public class FunderInsurancePlanEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("hcFunderId")]
        public int FunderId { get; set; }
        [Column("PlanName")]
        public string PlanName { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }

        public virtual FunderEntity Funder { get; set; }
    }
}
