using Rethink.Services.Common.Models;

namespace BillingService.Domain.Models
{
    public class ClientAuthorizationRequest : UserInfo
    {
        public int AuthorizationId { get; set; }
        public int ChildProfileId { get; set; }
        public string LocaleString { get; set; }
        public ListSortFilterModel ListSortModel { get; set; }
    }
}
