using Rethink.Services.Common.Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace RethinkAutism.Data.Entities.Curriculum
{
    public class FormEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("Id")]
        public int Id { get; set; }
        [Column("accountInfoId")]
        public int? AccountInfoId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("isActive")]
        public bool IsActive { get; set; }
        [Column("isDefault")]
        public bool IsDefault { get; set; }
        [Column("hcFunderId")]
        public int? FunderId { get; set; }
        [Column("hcFormTypeId")]
        public int FormTypeId { get; set; }





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


        public virtual IEnumerable<FormFunderEntity> FormFunders { get; set; }
        public virtual IEnumerable<FormResponseEntity> FormResponses { get; set; }
    }
}
