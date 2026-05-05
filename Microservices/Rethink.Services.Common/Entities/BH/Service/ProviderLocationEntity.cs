using System;
using System.Collections.Generic;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Company;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Entities.BH.Service
{
    public class ProviderLocationEntity : BasePersistEntity, IAuditedEntity
    {
        public int AccountInfoId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string Fax { get; set; }
        public int AddressId { get; set; }
        public bool IsMainLocation { get; set; }
        public bool IsBillingLocation { get; set; }
        public string AgencyName { get; set; }
        public string FederalTaxId { get; set; }
        public string NpiNumber { get; set; }
        public string TaxonomyCode { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string ProviderCommercialNumber { get; set; }
        public string StateLicenseNumber { get; set; }
        public string LocationNumber { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ICollection<ChildProfileEntity> ChildProfiles { get; set; }

        //[JsonIgnore]
        //public virtual ICollection<StaffMemberLocationEntity> StaffMemberLocations { get; } =
        //    new HashSet<StaffMemberLocationEntity>();

        //[NotMapped]
        //public IList<MemberEntity> Members => StaffMemberLocations.Select(m => m.Member).ToList();


        public virtual AddressEntity Address { get; set; }
    }
}