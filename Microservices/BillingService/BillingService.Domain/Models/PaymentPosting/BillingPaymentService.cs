using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class BillingPaymentMessage
    {
        public int PaymentId { get; set; }
        public decimal AmountReceived { get; set; }
        public int PatientId { get; set; }
        public int GuarantorId { get; set; }
        public int AccountId { get; set; }
        public int MemberId { get; set; }
        public string ReferenceNo { get; set; } = string.Empty;
        public decimal PaidAmount { get; set; }
    }
}
