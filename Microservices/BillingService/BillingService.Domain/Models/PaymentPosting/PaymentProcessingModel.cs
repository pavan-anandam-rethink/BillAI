using Rethink.Services.Common.Enums.Billing;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class PaymentProcessingModel
    {
        public int PaymentId { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string FileName { get; set; }
        public int UploadId { get; set; }
    }
}