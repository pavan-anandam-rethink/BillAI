using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class PaymentsResponseModel
    {
        public List<PaymentModel> Data { get; set; }
        public bool isRevSpringEnabled { get; set; } = false;
        public int TotalCount { get; set; }
    }
}