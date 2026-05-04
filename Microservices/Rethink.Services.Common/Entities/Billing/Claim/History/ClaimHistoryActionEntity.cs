using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.Billing.Claim.History
{
    public class ClaimHistoryActionEntity : BasePersistEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
