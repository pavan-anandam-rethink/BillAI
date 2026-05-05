namespace BillingService.Domain.Models
{
    public class AuthorizationModifiedModel : UserInfo
    {
        public int AuthorizationId { get; set; }
        public string NewValue { get; set; }
        public string OldValue { get; set; }
    }
}
