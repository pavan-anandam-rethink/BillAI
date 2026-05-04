using BillingService.Domain.DataObjects.Base;
using System.Collections.Generic;

namespace BillingService.Domain.Models
{
    public class ClaimOptions
    {
        public List<BasicOption> Clients { get; set; }
        public List<BasicOption> Locations { get; set; }
        public List<BasicOption> Members { get; set; }
        public List<BasicOption> LocationCodes { get; set; }
        public List<int> ClaimIds { get; set; }
        public List<BasicOption> UnitTypes { get; set; }

        public List<BasicOption> RenderingProviders { get; set; }
        public List<BasicOption> ReferringProviders { get; set; }
        public List<BasicOption> BillingProviders { get; set; }
        public List<BasicOption> ServiceFacilities { get; set; }
    }
}
