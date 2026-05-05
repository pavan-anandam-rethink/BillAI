using BillingService.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace BillingService.Domain.Extensions
{
    public static class OrderByWriteOffApplicationTypeExt
    {
        public static List<BillingClaimDetailsModel> OrderByWriteOffApplicationType(this IQueryable<BillingClaimDetailsModel> chargeEntriesToOrder, int? type)
        {
            switch (type)
            {
                case 1:
                    return chargeEntriesToOrder.Where(x => x.BalanceAmount > 0).OrderByDescending(x => x.DOS).ToList();
                case 2:
                    return chargeEntriesToOrder.Where(x => x.BalanceAmount > 0).OrderBy(x => x.DOS).ToList();
                case 3:
                    return chargeEntriesToOrder.Where(x => x.BalanceAmount > 0).OrderByDescending(x => x.BalanceAmount).ToList();
                case 4:
                    return chargeEntriesToOrder.Where(x => x.BalanceAmount > 0).OrderBy(x => x.BalanceAmount).ToList();
                default:
                    return chargeEntriesToOrder.Where(x => x.BalanceAmount > 0).ToList();
            }
        }
    }
}
