using Rethink.Services.Common.Entities.Base;
using System.Collections.Generic;

namespace Rethink.Services.Common.Entities.Billing.Payment
{
    public partial class PaymentClaimAttachmentTypeEntity : IEntity
    {
        public PaymentClaimAttachmentTypeEntity()
        {
            PaymentClaimAttachments = new HashSet<PaymentClaimAttachmentEntity>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string MimeType { get; set; }

        public virtual ICollection<PaymentClaimAttachmentEntity> PaymentClaimAttachments { get; set; }
    }
}
