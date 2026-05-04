using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.DataObjects.Billing
{
    [ExcludeFromCodeCoverage]
    public class ClaimItemWithPayments : Models.ClaimItem
    {
        public List<ClaimChargeInfoItem> ClaimChargeInfoItems { get; set; }
    }
}
