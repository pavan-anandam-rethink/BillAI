using Rethink.Services.Common.Models;
using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class GetPaymentsModel : UserInfo
    {
        public List<SortingModel> SortingModels { get; set; }
        public List<FilterModel> FilterModels { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}