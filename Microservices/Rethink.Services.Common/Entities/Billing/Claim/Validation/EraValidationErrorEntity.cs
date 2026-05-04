using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Enums.Billing;

namespace Rethink.Services.Common.Entities.Billing.Claim.Validation
{
    public class EraValidationErrorEntity : BasePersistEntity
    {
        public int GroupCodeId { get; set; }
        public int? AdjustmentCodeId { get; set; }
        public AdjustmentLevel AdjustmentLevel { get; set; }
        public string? EntityIdentifierCode { get; set; }
        public string? StcPosition { get; set; }

        public virtual ExternalCodeEntity GroupCode { get; set; }
        public virtual ExternalCodeEntity AdjustmentCode { get; set; }
        public virtual ClaimValidationErrorEntity ClaimValidationError { get; set; }
    }
}
