using System;

namespace BillingService.Domain.Models.Clients
{
    public class ClientContact
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string MiddleName { get; set; }

        public string LastName { get; set; }

        public string Phone { get; set; }

        public string Phone2 { get; set; }

        public string Phone3 { get; set; }

        public string SMSPhone { get; set; }

        public string Street1 { get; set; }

        public string Street2 { get; set; }

        public string ContactRelationship { get; set; }

        public string Email { get; set; }

        public bool? IsAutismCoveredBenefit { get; set; }

        public string City { get; set; }

        public string Town { get; set; }

        public int? StateId { get; set; }

        public string State { get; set; }

        public int? CountryId { get; set; }

        public string Country { get; set; }

        public string Zip { get; set; }

        public bool? SystemLogin { get; set; }

        public int? MemberId { get; set; }

        public string InsuranceNo { get; set; }

        public int ChildProfileId { get; set; }

        public int PersonId { get; set; }

        public int AddressId { get; set; }

        public bool? SendLoginInvite { get; set; }

        public bool? SameAsClientAddress { get; set; }

        public int? RelationshipToInsured { get; set; }

        public int? InsuranceType { get; set; }

        public string InsuranceTypeName { get; set; }

        public DateTime? DOB { get; set; }

        public int? GenderId { get; set; }

        public string GenderName { get; set; }

        public int? MaritalStatusId { get; set; }

        public string MaritalStatus { get; set; }

        public string InsurancePolicyNumber { get; set; }

        public string InsuranceGroupNumber { get; set; }

        public string Employer { get; set; }

        public decimal? IndividualDeductible { get; set; }

        public decimal? FamilyDeductible { get; set; }

        public decimal? IndividualMOOP { get; set; }

        public decimal? FamilyMOOP { get; set; }

        public decimal? Copay { get; set; }

        public int? Coinsurance { get; set; }

        public string MedicalRecordNumber { get; set; }

        public int? CopaymentTypeId { get; set; }

        public int? PayorType { get; set; }
        public string PayorTypeName { get; set; }

        public int? CreditCardTypeId { get; set; }

        public string Creditcardtype { get; set; }

        public string CreditCardNumber { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public int? CvvNumber { get; set; }

        public bool? AuthorizationToCharge { get; set; }

        public int? LoggedInMemeberId { get; set; }

        public int TimezoneId { get; set; }

        public string TimezoneName { get; set; }

        public DateTime? PolicyStartDate { get; set; }

        public DateTime? PolicyEndDate { get; set; }

        public string FunderName { get; set; }

        public int FunderMappingId { get; set; }

        public int? FunderCaseManagerId { get; set; }

        public string CaseManagerName { get; set; }

        public bool? IsInvited { get; set; }

        public bool? SendInvite { get; set; }

        public bool IsPrimary { get; set; }

        public string PayorName
        {
            get
            {
                switch (RelationshipToInsured)
                {
                    case 1:
                        return "Self";
                    case 2:
                        return "Child";
                    default:
                        return null;
                }
            }
        }

        public string InsuranceDisplayName
        {
            get
            {
                switch (InsuranceType)
                {
                    case 1:
                        return "Primary";
                    case 2:
                        return "Secondary";
                    case 3:
                        return "Tertiary";
                    default:
                        return "";
                }
            }
        }

        public string InsuredGenderName
        {
            get
            {
                switch (GenderId)
                {
                    case 1:
                        return "Male";
                    case 2:
                        return "Female";
                    default:
                        return null;
                }
            }
        }

        public string Fax { get; set; }

    }
}
