using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Models.Clients.History
{
    public class ClientHistoryChargeDetailsRequestModel
    {
        public ClientHistoryChargeDetailsRequest clientHistoryChargeDetailsRequest { get; set; }
        public ClientHistoryChargeFilterModel clientHistoryChargeFilterModel { get; set; }

    }
}
