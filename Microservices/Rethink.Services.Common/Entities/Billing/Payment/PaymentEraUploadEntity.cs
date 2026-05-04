using Rethink.Services.Common.Entities.Base;
using System;

namespace Rethink.Services.Common.Entities.Billing.Payment
{
    public class PaymentEraUploadEntity : BasePersistEntity, IAuditedEntity
    {
        public string FileName { get; set; }
        public string BlobFileName { get; set; }
        public double FileSize { get; set; }
        public string FileMimeType { get; set; }
        public string FilePath { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        //public int? ClearingHouseId { get; set; }

        public virtual PaymentEntity Payment { get; set; }

    }
}