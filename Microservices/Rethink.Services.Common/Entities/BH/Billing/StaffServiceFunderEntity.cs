using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.Billing
{
    public class StaffServiceFunderEntity: BasePersistEntity, IAuditedEntity, IOption
    {
        [Column("hcStaffServiceId")]
        public int StaffServiceId { get; set; }
        [Column("hcServiceFunderId")]
        public int ServiceFunderId { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        //public virtual MemberEntity CreatedByMember { get; set; }
        //public virtual StaffServiceEntity StaffService { get; set; }
        //public virtual ServiceFunderEntity ServiceFunder { get; set; }
        [NotMapped]
        public int Option { get => StaffServiceId; set => StaffServiceId = value; }
    }
}