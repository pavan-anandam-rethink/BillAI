using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class UnAllocatedPaymentsModel : UserInfo
    {
        public int? Id { get; set; }
        public int PaymentId { get; set; }
        public int ChildProfileId { get; set; }
        public decimal UnAllocatedAmount { get; set; }
        public int? GuarantorContactId { get; set; }
        public string? Notes { get; set; }
    }
}
