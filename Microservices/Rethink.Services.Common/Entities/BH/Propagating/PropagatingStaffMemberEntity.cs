using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;

namespace Rethink.Services.Common.Entities.BH.Propagating
{
    public class PropagatingStaffMemberEntity : BasePersistEntity, IAuditedEntity
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        [Column("hcStaffCertificationId")]
        public int? StaffCertificationId { get; set; }
        [Column("hcStaffTitleId")]
        public int? StaffTitleId { get; set; }
        public string NpiNumber { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? EndDate { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }


        //public virtual StaffTitleEntity StaffTitle { get; set; }
    }
}