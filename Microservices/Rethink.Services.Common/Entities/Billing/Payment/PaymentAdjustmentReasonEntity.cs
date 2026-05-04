using Rethink.Services.Common.Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Payment
{
    public class PaymentAdjustmentReasonEntity : BasePersistEntity, IAuditedEntity
    {
        public string GroupCode { get; set; }
        public string AdjustmentCode { get; set; }
        public string Description { get; set; }
        public bool ShowForNonEra { get; set; }
        public int ClaimResult { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public bool? IsDefault { get; set; }

        public IEnumerable<PaymentClaimAdjustmentEntity> PaymentClaimAdjustments { get; set; }
        //public IEnumerable<PaymentClaimServiceLineAdjustmentEntity> PaymentClaimServiceLineAdjustments { get; set; }
    }
}