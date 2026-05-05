using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Models.BulkPaymentPosting
{
    public class BulkPaymentPostingRequestModel : UserInfo
    {
        public int[] Ids { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}
