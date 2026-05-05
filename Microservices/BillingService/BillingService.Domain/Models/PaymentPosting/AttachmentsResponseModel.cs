using System.Collections.Generic;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class AttachmentsResponseModel
    {
        public List<AttachmentViewModel> Data { get; set; }
        public int TotalCount { get; set; }
    }
}