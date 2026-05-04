using System;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.Company
{
    public class TimeZoneEntity : BasePersistEntity, IAuditedEntity
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string SimpleName { get; set; }
        public int? DisplayOrder { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}