using System;

namespace Rethink.Services.Common
{
    public class ReferringProviderEntityModel
    {
        public int Id { get; set; }
        public int AccountInfoId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NationalProviderId { get; set; }
        public string TaxonomyCode { get; set; }
        public string Credential { get; set; }
        public string Facility { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }
        public int? StateID { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

    }
}