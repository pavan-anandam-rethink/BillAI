using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Enums.Billing
{
    public enum ClaimsTab
    {
        PendingReview = 1,
        ReadyToBill = 2,
        BilledPending = 3,
        Completed = 4,
        Rejected = 5,
        Denied = 6,
        Flagged = 7
    }
}
