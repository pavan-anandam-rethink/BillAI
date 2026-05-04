using Rethink.Services.Common.Enums.Billing;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.DataObjects.Billing
{
    [ExcludeFromCodeCoverage]
    public class PaymentDataForExpected
    {
        public int FunderId { get; set; }
        public PaymentTypes PaymentType { get; set; }
    }
}
