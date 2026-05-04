using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Enums.Billing;
using System;

namespace Rethink.Services.Common.Entities.Billing.Claim
{
    public class ClearingHouseResponseDetailsEntity : BasePersistEntity, IAuditedEntity
    {
        public ClaimResponseFileType ResponseFileTypeId { get; set; }
        public string BatchId { get; set; }
        public bool IsAccepted { get; set; }
        public int ClaimId { get; set; }
        public int ClaimValidationErrorId { get; set; }
        public int ClearingHouseId { get; set; }
        public string FileIdentifier { get; set; }
        public DateTime? DownloadDateTime { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
    }
}
