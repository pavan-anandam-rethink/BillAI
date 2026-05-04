using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing.PatientInvoice
{
    public class PatientGuarantorEntity : BasePersistEntity
    {
        public int InvoiceId { get; set; }

        // IDs
        public int GuarantorId { get; set; }   // contact id
        public int ClientId { get; set; }        // client/user id from payload
        public int AccountId { get; set; }
        public int? MemberId { get; set; }

        // Types/flags
        public string UserType { get; set; }               // e.g., "Client"
        public bool IsPrimaryContact { get; set; }
        public bool IsGuarantor { get; set; }

        // Name
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }

        // Contact
        public string Email { get; set; }
        public string Phone { get; set; }

        // Relationship
        public string RelationToClient { get; set; }       // e.g., "Parent"
        public int? RelationshipToInsured { get; set; }

        // Demographics
        public int? GenderId { get; set; }
        public int? MaritalStatusId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? TimezoneId { get; set; }

        // Insurance/records
        public string? MedicalRecordNumber { get; set; }
        public string? InsurancePolicyNumber { get; set; }
        public bool HasSystemLogin { get; set; }

        // Address snapshot
        public int AddressId { get; set; }
        public string Street1 { get; set; }
        public string? Street2 { get; set; }
        public string City { get; set; }
        public int? StateId { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public int? CountryId { get; set; }
        public string? Country { get; set; }
        public string? Town { get; set; }

        // Audit metadata (from payload metaData)
        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime UtcCreatedOn { get; set; }
        public DateTime UtcModifiedOn { get; set; }
        public DateTime? UtcDeletedOn { get; set; }

        public virtual PatientInvoiceEntity PatientInvoice { get; set; }
    }
}