using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.Curriculum
{
    public class WorkflowStatusEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("TypeId")]
        public override int Id { get; set; }

        public int StatusId { get; set; }

        public string StatusDescription { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
    }
}