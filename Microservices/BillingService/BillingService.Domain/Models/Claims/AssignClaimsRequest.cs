using BillingService.Domain.Models;

namespace BillingService.Web.Controllers
{
    public class AssignClaimsRequest : UserInfo
    {
        public int[] ClaimIds { get; set; }
        public int AssigneeId { get; set; }
    }
}