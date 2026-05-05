using System;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class PaymentSummary
    {
        public int Id { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal PaymentAmountOrig { get; set; }
        public decimal PostedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime? PostDate { get; set; }
        public DateTime? DepositDate { get; set; }
        public string FunderName { get; set; }
        public string PaymentMethod { get; set; }
        public int PaymentMethodId { get; set; }
        public string Payee { get; set; }
        public string ReferenceNumber { get; set; }
        public bool IsManual { get; set; }

        public int PaymentTypeId { get; set; }

    }
}