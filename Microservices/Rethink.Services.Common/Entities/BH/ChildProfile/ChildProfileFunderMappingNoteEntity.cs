using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.ChildProfile
{
    public class ChildProfileFunderMappingNoteEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("hcChildProfileFunderMappingId")]
        public int ChildProfileFunderMappingId { get; set; }
        public string Note { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ChildProfileFunderMappingEntity ChildProfileFunderMapping { get; set; }
    }
}
