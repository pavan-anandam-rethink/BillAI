namespace BillingService.Domain.Models
{
    public class UserInfo
    {
        public int AccountInfoId { get; set; }

        public int MemberId { get; set; }
    }

    public class ClientHistoryUserInfo : UserInfo
    {
        public int ClientId { get; set; }
    }
}