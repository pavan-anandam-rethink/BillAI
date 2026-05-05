using System.Collections.Generic;

namespace BillingService.Domain.Models.Claims
{
    public class ClaimUploadModelWithUserInfo : UserInfo
    {
        public byte[] Data { get; set; }
        public string FileName { get; set; }
        public string FileMimeType { get; set; }
        public int ClaimId { get; set; }
        public int? ClearingHouseId { get; set; }
    }
}