using BillingService.Domain.DataObjects.Billing;
using System.Collections.Generic;

namespace BillingService.Domain.Models
{
    public class ClaimHeaderModelResponseModel
    {
        public List<ClaimHeaderModel> Data { get; set; }
        public int TotalCount { get; set; }
        public ClaimsCountModel ClaimsCount { get; set; }
    }
}