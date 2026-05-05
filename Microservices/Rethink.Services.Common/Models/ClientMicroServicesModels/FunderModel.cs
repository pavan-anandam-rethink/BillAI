using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class FunderModel
    {
        public int Id { get; set; }
        public string FunderName { get; set; }
        public string VendorId { get; set; }
        public string Name { get; set; }
        public int funderTypeId { get; set; }
        public int accountId { get; set; }
        public int? billingProviderOptionId { get; set; }
        public MetaData metaData { get; set; }
        public int ChildProfileId { get; set; }
        public int ClientFunderId { get; set; }
        public int ChildProfileReferringProviderId { get; set; }
    }
    [Owned]
    public class FunderInsurancePlanModel
    {
        public int total { get; set; }
        public List<FunderInsurancePlan> data { get; set; }
    }
    [Owned]
    public class FunderInsurancePlan
    {
        public int funderId { get; set; }
        public string planName { get; set; }
        public string funder { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }
    }
}
