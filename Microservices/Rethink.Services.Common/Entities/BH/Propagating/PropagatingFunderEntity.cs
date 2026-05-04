using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.Propagating
{
    public class PropagatingFunderEntity : BasePersistEntity, IAuditedEntity
    {
        public string FunderName { get; set; }
        public string Phone { get; set; }
        public string Street1 { get; set; }
        public string City { get; set; }
        public int? StateId { get; set; }
        public string Zip { get; set; }
        public string VendorId { get; set; }
        public bool? BillingCombineCharges { get; set; }
        [Column("hcProviderLocationId")]
        public int? ProviderLocationId { get; set; }
        public string Town { get; set; }
        public int? CountryId { get; set; }
        public DateTime? EndDate { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}