using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Models.ClientMicroServicesModels
{
    public class RethinkGuarantorDetails
    {
        public class ClientModel
        {
                public int Id { get; set; }
                public int UserId { get; set; }
                public string UserType { get; set; }
                public Name Name { get; set; }
                public Address Address { get; set; }
                public string Email { get; set; }
                public string PhoneNumber { get; set; }
                public string RelationToClient { get; set; }
                public string RelationshipToInsured { get; set; }
                public int? TimezoneId { get; set; }
                public bool IsPrimaryContact { get; set; }
                public bool IsGuarantor { get; set; }
                public int? GenderId { get; set; }
                public int? MaritalStatusId { get; set; }
                public DateTime? DateOfBirth { get; set; }
                public string MedicalRecordNumber { get; set; }
                public string InsurancePolicyNumber { get; set; }
                public int AccountId { get; set; }
                public bool HasSystemLogin { get; set; }
                public string MemberId { get; set; }
                public List<object> Identifiers { get; set; }
                public MetaData MetaData { get; set; }
            }

            public class Name
            {
                public string FirstName { get; set; }
                public string MiddleName { get; set; }
                public string LastName { get; set; }
                public string Prefix { get; set; }
                public string Suffix { get; set; }
            }

            public class Address
            {
                public int Id { get; set; }
                public string Street1 { get; set; }
                public string Street2 { get; set; }
                public string City { get; set; }
                public int? StateId { get; set; }
                public string State { get; set; }
                public string ZipCode { get; set; }
                public int? CountryId { get; set; }
                public string Country { get; set; }
                public string Town { get; set; }
                public object MetaData { get; set; }
            }

            public class MetaData
            {
                public DateTime CreatedOn { get; set; }
                public int CreatedBy { get; set; }
                public DateTime ModifiedOn { get; set; }
                public int ModifiedBy { get; set; }
                public DateTime? DeletedOn { get; set; }
                public int? DeletedBy { get; set; }
                public DateTime UtcCreatedOn { get; set; }
                public DateTime UtcModifiedOn { get; set; }
                public DateTime? UtcDeletedOn { get; set; }
            }
    }
}
