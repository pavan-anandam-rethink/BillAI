using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Billing;

namespace Rethink.Services.Common.Entities.BH.Service
{
    public class ServiceFunderEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("hcFunderId")]
        public int FunderId { get; set; }
        [Column("hcProviderServiceId")]
        public int ProviderServiceId { get; set; }
        public bool IsActive { get; set; }
        [Column("hcServiceNPIBillingTypeId")]
        public int? ServiceNPIBillingTypeId { get; set; }
        public int? StaffProviderId { get; set; }
        [Column("hcBillingSubmissionMethodId")]
        public int? BillingSubmissionMethodId { get; set; }
        [Column("hcBillingProviderOptionId")]
        public int? BillingProviderOptionId { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual FunderEntity Funder { get; set; }
        //    public virtual List<StaffServiceFunderEntity> StaffServiceFunders { get; set; }
        //    public virtual ProviderServiceLineEntity ProviderServiceLine { get; set; }
    }
}