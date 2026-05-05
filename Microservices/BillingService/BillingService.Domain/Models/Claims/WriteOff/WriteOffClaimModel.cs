namespace BillingService.Domain.Models.Claims
{
    public class WriteOffClaimModelWithUserInfo : UserInfo
    {
        public int ClaimId { get; set; }
        public int? ServiceLineId { get; set; }
        public int AmountTypeId { get; set; }
        public decimal Amount { get; set; }
        public int? ApplicationTypeId { get; set; }
        public int ReasonCodeId { get; set; }
        public string Note { get; set; }
        public bool? IsServiceLine { get; set; }
    }
}
