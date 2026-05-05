using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using System;

namespace Rethink.Services.Common.Entities.Billing.Payment
{
    public class ChargePaymentEntity : BasePersistEntity, IAuditedEntity
    {
        public int ChargeId { get; set; }
        public decimal Amount { get; set; }
        public int ReasonCodeId { get; set; }
        public int PaymentMethodId { get; set; }
        public string Reference { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ClaimChargeEntryEntity ChargeEntry { get; set; }

        public virtual ClientReasonCodes ReasonCode { get; set; }

        public virtual PaymentMethodEntity PaymentMethod { get; set; }

        public virtual RethinkAccountMember CreatedMember { get; set; }

        //public int EntityTypeId => 8;

        //[NotMapped]
        //public int? TypeId { get; set; }

        //public List<string> PropertiesToExclude => 
        //    new List<string>() { "CreatedBy", "ModifiedBy", "DateCreated", "DateLastModified" };
    }
}