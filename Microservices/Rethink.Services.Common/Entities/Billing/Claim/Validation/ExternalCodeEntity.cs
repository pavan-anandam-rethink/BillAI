using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Enums.Billing;

namespace Rethink.Services.Common.Entities.Billing.Claim.Validation
{
    public class ExternalCodeEntity : BasePersistEntity
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public ExternalCodeType CodeTypeId { get; set; }
    }
}
