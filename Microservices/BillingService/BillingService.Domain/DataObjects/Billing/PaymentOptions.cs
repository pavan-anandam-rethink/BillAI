using BillingService.Domain.DataObjects.Base;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.DataObjects.Billing
{
    [ExcludeFromCodeCoverage]
    public class PaymentOptions
    {
        public List<BaseNameOption> Charges { get; set; }

        public List<BaseNameOption> Reasons { get; set; }

        public List<BaseNameOption> PaymentMethods { get; set; }

        public List<BaseNameOption> References { get; set; }
    }
}
