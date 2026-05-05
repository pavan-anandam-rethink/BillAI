using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Billing;
using RethinkAutism.Data.Entities.Billing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RethinkAutism.Data.Entities.Curriculum
{
    public class FormFunderEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("Id")]
        public int Id { get; set; }
        [Column("hcFormId")]
        public int FormId { get; set; }
        [Column("hcFunderId")]
        public int FunderId { get; set; }




        [Column("CreatedBy")]
        public int CreatedBy { get; set; }
        [Column("ModifiedBy")]
        public int? ModifiedBy { get; set; }
        [Column("DeletedBy")]
        public int? DeletedBy { get; set; }
        [Column("DateCreated")]
        public DateTime DateCreated { get; set; }
        [Column("DateLastModified")]
        public DateTime? DateLastModified { get; set; }
        [Column("DateDeleted")]
        public DateTime? DateDeleted { get; set; }



        public virtual FormEntity Form { get; set; }
        public virtual FunderEntity Funder { get; set; }

    }
}
