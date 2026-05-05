using BillingService.Domain.DataObjects.Base;

namespace BillingService.Domain.Models
{
    public class ClientOptionModel : BaseNameOption
    {
        public int? FacilityId { get; set; }
    }
}
