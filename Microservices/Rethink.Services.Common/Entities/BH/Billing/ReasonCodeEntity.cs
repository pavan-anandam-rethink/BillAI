using System;
using System.ComponentModel.DataAnnotations;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.Billing
{
    public class ReasonCodeEntity: BasePersistEntity, IAuditedEntity
    {
    
        [Required]
        public string Name { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}