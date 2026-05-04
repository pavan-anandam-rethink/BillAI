using Rethink.Services.Common.Entities.Base;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClaimFlagTransaction : BasePersistEntity
    {
        public int AccountInfoId { get; set; }
        public int HcClaimId { get; set; }
        public int ReasonId { get; set; }
        public string? Comment { get; set; }
        [RegularExpression("Flagged|Unflagged|Updated")]
        public string ActionType { get; set; } = null!; // Only allowed values
        public DateTime DateCreated { get; set; }

        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }

        public virtual ClaimFlagReasonMaster Reason { get; set; } = null!;
        public virtual ClaimEntity Claim { get; set; } = null!;
    }
}
