using System;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.Service
{
    public class ProviderServiceEntity: BasePersistEntity, IAuditedEntity
    {
        public int AccountInfoId { get; set; }
        public string Name { get; set; }
        public decimal BaseRate { get; set; }
        public bool IsActive { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        //public virtual List<ChildProfileAuthorizationBillingCodeEntity> 
        //    ChildProfileAuthorizationBillingCodes { get; set; }
    }
}