using Microsoft.EntityFrameworkCore;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class RethinkProviderBillingCode
    {
        public string billingCode { get; set; }
        public string billingCode2 { get; set; }
        public string billingCodeText { get; set; }
        public decimal? rate { get; set; }
        public int serviceId { get; set; }
        public int unitTypeId { get; set; }
        public int funderId { get; set; }

        public int id { get; set; }
    }
}
