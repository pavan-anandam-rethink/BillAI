using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Scheduling;

namespace Rethink.Services.Common.Entities.BH.Curriculum
{
    public class WorkflowHistoryEntity: BasePersistEntity, IAuditedEntity
    {
        [Column("Id")]
        public int Id { get; set; }
        [Column("TypeId")]
        public int TypeId { get; set; }
        [Column("StatusId")]
        public int StatusId { get; set; }
        [Column("ReferenceId")]
        public int ReferenceId { get; set; }
        [Column("ExpirationDate")]
        public DateTime? ExpirationDate { get; set; }

        [Column("CreatedDate")]
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        [Column("UpdatedBy")]
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }

        public virtual AppointmentEntity Appointment { get; set; }
        public virtual WorkflowStatusEntity Status { get; set; }
    }
}