using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class UnAllocatedPaymentRequestModel : UserInfo
    {
        public int PaymentId { get; set; }
        public int ChildProfileId { get; set; }
    }
}
