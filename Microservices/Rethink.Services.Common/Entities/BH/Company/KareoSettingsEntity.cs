using System;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.Company
{
    public class KareoSettingsEntity : BasePersistEntity, IAuditedEntity
    {
        public int AccountInfoId { get; set; }
        public bool? CombineMultiple { get; set; }
        public int? PracticeId { get; set; }
        public string CustomerId { get; set; }
        public string CustomerKey { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}