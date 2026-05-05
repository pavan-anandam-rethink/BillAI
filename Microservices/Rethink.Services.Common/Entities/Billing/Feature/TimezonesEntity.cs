using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing.Feature
{
    public class TimezonesEntity : BasePersistEntity
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string SimpleName { get; set; }
        public int DisplayOrder { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? DateDeleted { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime DateCreated { get; set; }
        public int? SandataTimezoneId { get; set; }
    }
}
