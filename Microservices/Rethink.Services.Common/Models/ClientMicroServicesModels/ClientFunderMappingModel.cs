using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ClientFunderMappingModel
    {
        public int total { get; set; }
        public List<ClientFunders> data { get; set; }
        public MetaData metaData { get; set; }
    }
    [Owned]
    public class ClientFunders
    {
        public int childProfileId { get; set; }
        public int funderId { get; set; }
        public int? funderCaseManagerId { get; set; }
        public int childProfileInsuranceContactId { get; set; }
        public string clientFunderMrnNo { get; set; }
        public string caseNumber { get; set; }
        public string? serviceContractDetails { get; set; }
        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }
        public int releaseOfInformationConfirmationTypeId { get; set; }
        public DateTime? releaseOfInformationConfirmationDate { get; set; }
        public int authorizedPaymentConfirmationTypeId { get; set; }
        public string? caseNumber2 { get; set; }
        public int? insurancePolicyId { get; set; }
        public int? insuranceCaseNumber { get; set; }
        public int? funderUnsurancePlanId { get; set; }
        public bool? isDph { get; set; }
        public bool? isAutismCoveredBenefit { get; set; }
    }
}
