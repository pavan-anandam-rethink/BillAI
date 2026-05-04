using Rethink.Services.Common.Models;
using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class GetByIdSortFilterWithUserInfo : UserInfo
    {
        public int Id { get; set; }
        public List<SortingModel> SortingModels { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}