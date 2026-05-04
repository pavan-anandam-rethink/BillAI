using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Scheduling;

namespace Rethink.Services.Common.Entities.BH.Service
{
    public class ProviderBillingCodeCredentialEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("hcProviderBillingCodeId")]
        public int ProviderBillingCodeId { get; set; }
        [Column("hcStaffCertificationLUId")]
        public int StaffCertificationLUId { get; set; }
        [Column("hcStaffCredentialId")]
        public int? StaffCredentialId { get; set; }
        public decimal ContractRate { get; set; }
        public bool IsPrimary { get; set; }
        public string Modifier1 { get; set; }
        public string Modifier2 { get; set; }
        public string Modifier3 { get; set; }
        public string Modifier4 { get; set; }
        public string Modifier2Name { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }


        public virtual ProviderBillingCodeEntity ProviderBillingCode { get; set; }
        public virtual ICollection<AppointmentEntity> Appointments { get; set; }
        //public virtual StaffCredentialEntity StaffCredential { get; set; }
    }
}