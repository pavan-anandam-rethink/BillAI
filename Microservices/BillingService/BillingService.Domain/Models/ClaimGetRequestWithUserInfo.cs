using Rethink.Services.Common.Models;
using System.Collections.Generic;

namespace BillingService.Domain.Models
{
    public class ClaimGetRequestSortFilterWithUserInfo : UserInfo
    {
        public List<SortingModel> SortingModels { get; set; }
        public ClaimFiltersModel Filters { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}