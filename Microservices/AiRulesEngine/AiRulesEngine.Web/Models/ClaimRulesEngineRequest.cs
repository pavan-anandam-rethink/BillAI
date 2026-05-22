using System;

namespace AiRulesEngine.Web.Models
{
    public class ClaimRulesEngineRequest
    {
        public string RuleSetId { get; set; }
        public ClaimRulesEngineContext Context { get; set; }
    }

    public class ClaimRulesEngineContext
    {
        public ClaimContext Claim { get; set; }
        public AccountContext Account { get; set; }
        public FunderContext Funder { get; set; }
        public BillingProviderContext BillingProvider { get; set; }
        public AddressContext BillingProviderAddress { get; set; }
        public AddressContext ServiceLocationAddress { get; set; }
        public ChildProfileContext ChildProfile { get; set; }
    }

    public class ClaimContext
    {
        public int ClaimId { get; set; }
        public int AccountInfoId { get; set; }
        public string ClaimStatus { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ClaimIdentifier { get; set; }
    }

    public class AccountContext
    {
        public string BillingAddress1 { get; set; }
        public string BillingCity { get; set; }
        public string BillingState { get; set; }
        public string BillingZip { get; set; }
    }

    public class FunderContext
    {
        public int? FunderId { get; set; }
        public string FunderName { get; set; }
    }

    public class BillingProviderContext
    {
        public string NpiNumber { get; set; }
        public string TaxId { get; set; }
        public string Name { get; set; }
        public string TaxonomyCode { get; set; }
    }

    public class ChildProfileContext
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string ZipCode { get; set; }
    }

    public class AddressContext
    {
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
    }
}
