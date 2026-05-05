namespace BillingService.Domain.Models
{
    public class ClaimAttachmentModelWithUserInfo : UserInfo
    {
        public ClaimAttachmentModel ClaimAttachmentModel { get; set; }
    }
}