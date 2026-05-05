using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Models.Clients.History
{
    public class ClientHistoryRequestModel
    {
        public ClientHistoryRequest clientHistoryRequest { get; set; }
        public ClientRecordFilterModel clientRecordFilterModel { get; set; }
    }
}
