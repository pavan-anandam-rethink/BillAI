using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    public class ClientBillingCodesModel
    {
        public int total { get; set; }
        public List<BillingCodes> data { get; set; }
    }
    public class BillingCodes
    {
        public int accountId { get; set; }
        public int funderId { get; set; }
        public int providerServiceId { get; set; }
        public string billingCode { get; set; }
        public string description { get; set; }
        public int unitTypeId { get; set; }
        public int frequencyTypeId { get; set; }
        public decimal rate { get; set; }
        public string modifier { get; set; }
        public string billingCode2 { get; set; }
        public int unitTypeId2 { get; set; }
        public decimal rate2 { get; set; }
        public int duration { get; set; }
        public int durationTypeId { get; set; }
        public bool combined { get; set; }
        public int billingCodeTemplateId { get; set; }
        public int serviceId { get; set; }
        public bool restrictStaffProviderToService { get; set; }
        public int providerBillingCodeRateTypeId { get; set; }
        public int providerBillingCodeRoundingTypeId { get; set; }
        public int providerBillingCodeRoundingTypeId2 { get; set; }
        public DateTime propagatingEndDate { get; set; }
        public bool inactive { get; set; }
        public bool noAuthRequired { get; set; }
        public int? renderingProviderTypeId { get; set; }
        public int? renderingProviderStaffId { get; set; }
        public int id { get; set; }

        public MetaData metaData { get; set; }
    }
    public class ClientAuthBillingCodesModel
    {
        public int total { get; set; }
        public List<ClientAuthBillingCodes> data { get; set; }
    }
    public class ClientAuthBillingCodes
    {
        public int childProfileAuthorizationId { get; set; }
        public int providerBillingCodeId { get; set; }
        public int noOfUnits { get; set; }
        public int unitTypeId { get; set; }
        public int frequencyTypeId { get; set; }
        public int schedulingGoalNoOfUnits { get; set; }
        public int schedulingGoalFrequencyTypeId { get; set; }
        public int providerServiceId { get; set; }
        public int id { get; set; }

        public MetaData metaData { get; set; }
    }

}
