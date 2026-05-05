namespace BillingService.Domain.Models.PaymentPosting
{
    public class EraUploadModelWithUserInfo : UserInfo
    {
        public byte[] Data { get; set; }
        public string FileName { get; set; }
        public string FileMimeType { get; set; }
    }
}