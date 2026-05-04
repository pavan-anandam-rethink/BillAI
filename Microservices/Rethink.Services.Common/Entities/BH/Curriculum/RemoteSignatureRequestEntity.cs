using Rethink.Services.Common.Entities.Base;
using System;

namespace RethinkAutism.Data.Entities.Curriculum
{
    public class RemoteSignatureRequestEntity : BasePersistEntity, IAuditedEntity
    {
        public int Id { get; set; }
        public int AssignedMemberId { get; set; }
        public int Status { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
    }
}
