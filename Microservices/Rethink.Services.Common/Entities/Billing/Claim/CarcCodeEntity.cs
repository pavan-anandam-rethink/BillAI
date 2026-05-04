using System;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class CarcCodeEntity : BasePersistEntity
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}
