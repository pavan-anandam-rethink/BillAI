using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Service;
using Rethink.Services.Common.Enums.BH;

namespace Rethink.Services.Common.Entities.BH.ChildProfile
{
    public class ChildProfileFunderServiceLineMappingEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("hcChildProfileFunderMappingId")]
        public int ChildProfileFunderMappingId { get; set; }
        [Column("hcServiceId")]
        public int ServiceId { get; set; }
        public ResponsibilitySequenceType responsibilitySequence { get; set; } = ResponsibilitySequenceType.Primary; // P = Primary, S = Secondary, T = Tertiary, 4, 5, 6, 7, 8, 9
        //public ResponsibilitySequenceType responsibilitySequence { get { return ResponsibilitySequenceType.Primary; } set { responsibilitySequence = value; } } // #DBMIGRATION
        public bool BypassPrimary { get; set; }


        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ChildProfileFunderMappingEntity ChildProfileFunderMapping { get; set; }
        public virtual ProviderServiceLineEntity ProviderSeviceLine { get; set; }
    }
}