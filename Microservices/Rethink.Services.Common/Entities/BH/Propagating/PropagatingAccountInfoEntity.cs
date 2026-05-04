using System;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.Propagating
{
    public class PropagatingAccountInfoEntity : BasePersistEntity, IAuditedEntity
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string FederalTaxId { get; set; }
        public string NationalProviderId { get; set; }
        public DateTime? EndDate { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}