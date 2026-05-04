using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Models.Clients
{
    public class ClientHistoryChargeFilterModel
    {
        // Required date range filter with defaults
        public DateTime FromDate { get; set; } = DateTime.Today.AddDays(-90);
        public DateTime ThroughDate { get; set; } = DateTime.Today;

        // Optional filters - nullable or string (null means no filter)
        public List<int> PlaceOfService { get; set; } = null;
        public List<int> RenderingProvider { get; set; } = null;
        public List<int> PrimaryFunder { get; set; } = null;
        public List<int> AuthorizationNumber { get; set; }
    }
}
