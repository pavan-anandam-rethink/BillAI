using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using System.Collections.Generic;
using System.Linq;

namespace BillingService.Domain.Extensions
{
    public static class PaymentClaimServiceLineExt
    {
        public static List<PaymentClaimServiceLineEntity> OrderByApplicationType(this ICollection<PaymentClaimServiceLineEntity> serviceLinesEntitiesToOrder, BulkPostingCriteria? type)
        {
            switch (type)
            {
                case BulkPostingCriteria.HighestToLowest:
                    return serviceLinesEntitiesToOrder.OrderByDescending(x =>
                    {
                        var claimLineBalance = x.ChargeAmount -
                            (x.PaymentClaimServiceLineAdjustments
                                .Sum(y => y.AdjustmentAmount) ?? 0) ?? 0;

                        return claimLineBalance;
                    }).ToList();
                case BulkPostingCriteria.LowestToHighest:
                    return serviceLinesEntitiesToOrder.OrderBy(x =>
                    {
                        var claimLineBalance = x.ChargeAmount -
                            (x.PaymentClaimServiceLineAdjustments
                                .Sum(y => y.AdjustmentAmount) ?? 0) ?? 0;

                        return claimLineBalance;
                    }).ToList();
                case BulkPostingCriteria.NewestToOldest:
                    return serviceLinesEntitiesToOrder.OrderByDescending(x => x.DateOfService).ToList();
                case BulkPostingCriteria.OldestToNewest:
                    return serviceLinesEntitiesToOrder.OrderBy(x => x.DateOfService).ToList();
                default:
                    return serviceLinesEntitiesToOrder.ToList();
            }
        }
    }
}
