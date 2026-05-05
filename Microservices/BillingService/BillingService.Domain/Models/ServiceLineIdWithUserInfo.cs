using BillingService.Domain.Models.PaymentPosting;

namespace BillingService.Domain.Models
{
    public class ServiceLineIdWithUserInfo : IdWithUserInfo
    {
        public int ServiceLineId { get; set; }
    }
}