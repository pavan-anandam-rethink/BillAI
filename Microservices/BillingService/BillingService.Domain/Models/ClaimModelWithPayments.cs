using BillingService.Domain.DataObjects.Billing;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class ClaimModelWithPayments : ClaimModel
    {
        public List<ClaimChargeInfoItem> ClaimChargeInfoItems { get; set; }
    }
}