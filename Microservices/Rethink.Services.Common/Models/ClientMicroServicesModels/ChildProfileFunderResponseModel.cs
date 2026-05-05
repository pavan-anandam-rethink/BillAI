using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Enums.BH;
using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ChildProfileFunderResponseModel
    {
        public int total { get; set; }
        public List<FunderDetails> data { get; set; }
    }
    [Owned]
    public class FunderDetails
    {
        public int childProfileId { get; set; }
        public int funderId { get; set; }
        public int? funderCaseManagerId { get; set; }
        public int childProfileInsuranceContactId { get; set; }
        public string clientFunderMrnNo { get; set; }
        public string caseNumber { get; set; }
        public string serviceContractDetails { get; set; }
        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }
        public int? releaseOfInformationConfirmationTypeId { get; set; }
        public DateTime? releaseOfInformationConfirmationDate { get; set; }
        public int? authorizedPaymentConfirmationTypeId { get; set; }
        public string caseNumber2 { get; set; }
        public int? insurancePolicyId { get; set; }
        public string insuranceCaseNumber { get; set; }
        public ResponsibilitySequenceType insuranceType { get; set; }
        public int? funderUnsurancePlanId { get; set; }
        public bool? isDph { get; set; }
        public bool? isAutismCoveredBenefit { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }
        public FunderDataModel Funder { get; set; }
        public InsuranceContacts InsuranceContact { get; set; }
    }

    [Owned]
    public class ServiceFunderData
    {
        public int funderId { get; set; }
        public int id { get; set; }
        public int providerServiceId { get; set; }
        public int? billingProviderOptionId { get; set; }
        public MetaData metaData { get; set; }
    }
    [Owned]
    public class ServiceFunderDetails
    {
        public int total { get; set; }
        public List<ServiceFunderData> data { get; set; }
    }
}
