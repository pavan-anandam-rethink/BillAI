using Rethink.Services.Common.Models;
using System.Collections.Generic;

namespace BillingService.Domain.Models
{
    public class GetBillingClaimDetailsModel
    {
        public int ClaimId { get; set; }
        public int? ChargeEntryId { get; set; }
        public int AccountId { get; set; }
        public List<SortingModel> SortingModels { get; set; }
        public List<FilterModel> Filters { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}
