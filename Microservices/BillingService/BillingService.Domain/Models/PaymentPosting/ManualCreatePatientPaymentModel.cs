using System;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class ManualCreatePaymentModel : UserInfo
    {
        public string FunderType { get; set; }
        public string PaymentMethod { get; set; }
        public decimal PaymentAmount { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime? PostDate { get; set; }
        public DateTime? DepositDate { get; set; }
        public int? FunderId { get; set; }
    }

    public class ManualCreatePaymentModelRequest : ManualCreatePaymentModel
    {
        public int? PatientId { get; set; }
        public decimal? UnAllocatedAmount { get; set; }
        public string? Notes { get; set; }
    }
}
