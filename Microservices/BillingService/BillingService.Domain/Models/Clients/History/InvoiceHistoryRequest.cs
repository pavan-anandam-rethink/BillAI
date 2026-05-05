using Rethink.Services.Common.Models;
using System.Collections.Generic;

namespace BillingService.Domain.Models.Clients.History
{
    public class InvoiceHistoryRequest
    {
        public int ClientId { get; set; }
        public int Take { get; set; }
        public int Skip { get; set; }
        public List<SortingModel> SortingModels { get; set; }
    }
}