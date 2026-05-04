using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Models.Clients.History
{
    public class ClientHistoryRequest
    {
        public int AccountInfoId { get; set; }
        public int MemberId { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 20;
        public List<SortingModel> SortingModels { get; set; }
    }
}
