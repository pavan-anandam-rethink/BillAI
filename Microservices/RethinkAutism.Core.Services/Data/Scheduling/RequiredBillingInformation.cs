using System;
using System.Collections.Generic;
using RethinkAutism.Contracts.DataObjects.Billing;
using RethinkAutism.Core.Services.Model;

namespace RethinkAutism.Core.Services.Data.Scheduling
{
    public class RequiredBillingInformation
    {
        public int ClientId { get; set; }
        public int FunderId { get; set; }
        public int? FunderCoverageTypeId { get; set; }
        public string ClientAddress { get; set; }
        public string ClientCity { get; set; }
        public bool? IsInternationalAccount { get; set; }
        public int? ClientStateId { get; set; }
        public int? ClientCountryId { get; set; }
        public string ClientZipCode { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? GenderId { get; set; }
        public string ContactEncryptedFirstName { get; set; }
        public string ContactEncryptedLastName { get; set; }
        public string ContactInsurancePolicyNumber { get; set; }
        public int? ContactRelationshipToInsured { get; set; }
        public string ContactAddressStreet { get; set; }
        public string ContactAddressCity { get; set; }
        public int? ContactAddressStateId { get; set; }
        public int? ContactAddressCountryId { get; set; }
        public string ContactAddressZip { get; set; }
        public int? ReleaseOfInformationConfirmationTypeId { get; set; }
        public DateTime? ReleaseOfInformationConfirmationDate { get; set; }
        public int? AuthorizedPaymentConfirmationTypeId { get; set; }
        public bool ShowScheduling { get; set; }
        public bool ShowBilling { get; set; }
    }
}
