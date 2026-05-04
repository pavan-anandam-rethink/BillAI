using System;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.Billing
{
    public class ClearingHouseEntity: BasePersistEntity, IAuditedEntity
    {
        public string TaxId { get; set; }
        public string Title { get; set; }
        public bool? IsDefault { get; set; }

        public int ConnectionTypeId { get; set; }
        public string Notes { get; set; }
        public string UrlLink { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        //public string ProviderIdentifier { get; set; }
        //public string ClearingHouseIdentifier { get; set; }
        //public string SubmitterName { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}