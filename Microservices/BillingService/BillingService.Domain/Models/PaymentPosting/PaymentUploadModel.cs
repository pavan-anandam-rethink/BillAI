namespace BillingService.Domain.Models.PaymentPosting
{
    public class PaymentUploadModelWithUserInfo : UserInfo
    {
        public byte[] Data { get; set; }
        public string FileName { get; set; }
        public string FileMimeType { get; set; }
        public int PaymentId { get; set; }
    }
}