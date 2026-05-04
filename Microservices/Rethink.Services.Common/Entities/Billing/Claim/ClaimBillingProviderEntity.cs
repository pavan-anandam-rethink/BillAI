using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimBillingProviderEntity : BasePersistEntity, IAuditedEntity
    {
        public int ClaimId { get; set; }

        public string ProviderType { get; set; }  // Entity / Person

        public string? FirstName { get; set; }

        public string LastNameOrFacilityName { get; set; }

        public string NPI { get; set; } 

        public string? TaxId { get; set; }

        public string? TaxonomyCode { get; set; }

        public string AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string City { get; set; } 

        public string State { get; set; } 

        public string Zip { get; set; }

        public string ZipExt { get; set; } 

        public DateTime DateCreated { get; set; }

        public int CreatedBy { get; set; }

        public DateTime? DateLastModified { get; set; }

        public int? ModifiedBy { get; set; }

        public DateTime? DateDeleted { get; set; }

        public int? DeletedBy { get; set; }

        // Navigation Property of ClaimEntity
        public virtual ClaimEntity Claim { get; set; }
    }
}
