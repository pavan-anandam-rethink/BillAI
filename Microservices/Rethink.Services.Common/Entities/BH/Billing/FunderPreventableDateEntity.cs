using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Billing;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace RethinkAutism.Data.Entities.Billing
{
    public class FunderPreventableDateEntity : BasePersistEntity, IAuditedEntity
    {
        public int Id { get; set; }
        [Column("hcFunderId")]
        public int FunderId { get; set; }
        public DateTime AppointmentDate { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }


        public virtual FunderEntity Funder { get; set; }
    }
}
