using System;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.Company
{
    public class LocationCodeEntity : BasePersistEntity, IAuditedEntity
    {
        public string Description { get; set; }
        public string Code { get; set; }
        public int? OrderBy { get; set; }
        public bool? IsActive { get; set; } = true;

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}