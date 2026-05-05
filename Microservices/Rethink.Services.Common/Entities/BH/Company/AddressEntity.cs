using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.BH.Company
{
    public class AddressEntity : BasePersistEntity, IAuditedEntity
    {
        public string Street1 { get; set; }
        public string Street2 { get; set; }
        public string City { get; set; }
        public int? StateId { get; set; }
        public string Zip { get; set; }
        public int? CountryId { get; set; }
        public string Town { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual StateEntity StateLU { get; set; }
        public virtual CountryEntity CountryLU { get; set; }
    }
}