using System.IO;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class PaymentAttachmentReturnModel
    {
        public MemoryStream MemoryStream { get; set; }
        public string Filename { get; set; }
    }
}