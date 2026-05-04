namespace BillingService.Domain.Models
{
    public class ChargePaymentModelWithUserInfo : UserInfo
    {
        public ChargePaymentModel ChargePaymentModel { get; set; }
    }
}