using System.ComponentModel.DataAnnotations;
namespace BillingService.Domain.Models.PaymentPosting
{
    public class RenameAttachmentModelWithUserInfo : UserInfo
    {
        public int AttachmentId { get; set; }

        [StringLength(200, ErrorMessage = "FileName cannot exceed 200 characters.")]
        public string FileName { get; set; }
    }
}