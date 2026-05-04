using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing.Reporting
{
    public class ClaimStatusEntity : BasePersistEntity
    {
        public int claimStatusId { get; set; }
        public string claimStatus { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime? DateDeleted { get; set; }
    }
}
