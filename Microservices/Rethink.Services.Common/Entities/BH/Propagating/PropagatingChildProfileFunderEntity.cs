using System;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.Propagating
{
    public class PropagatingChildProfileFunderEntity : BasePersistEntity, IAuditedEntity
    {
        public int? RelationshipToInsured { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string Street1 { get; set; }
        public string City { get; set; }
        public int? StateId { get; set; }
        public string Zip { get; set; }     
        public DateTime? DOB { get; set; }
        public int? GenderId { get; set; }
        public int? MaritalStatusId { get; set; }
        public string InsurancePolicyNumber { get; set; }
        public string InsuranceGroupNumber { get; set; }
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