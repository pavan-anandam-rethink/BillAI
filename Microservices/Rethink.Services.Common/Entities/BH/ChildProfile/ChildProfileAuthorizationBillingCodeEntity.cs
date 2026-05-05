using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Company;
using Rethink.Services.Common.Entities.BH.Scheduling;
using Rethink.Services.Common.Entities.BH.Service;
using Rethink.Services.Common.Enums.BH;

namespace Rethink.Services.Common.Entities.BH.ChildProfile
{
    public class ChildProfileAuthorizationBillingCodeEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("hcChildProfileAuthorizationId")]
        public int ChildProfileAuthorizationId { get; set; }
        [Column("hcProviderBillingCodeId")]
        public int ProviderBillingCodeId { get; set; }
        public int NoOfUnits { get; set; }
        [Column("hcUnitTypeId")]
        public int UnitTypeId { get; set; }
        [Column("hcFrequencyTypeId")]
        public FrequencyTypes? FrequencyType { get; set; }
        public int? SchedulingGoalNoOfUnits { get; set; }
        public FrequencyTypes? SchedulingGoalFrequencyTypeId { get; set; }
        [Column("hcProviderServiceId")]
        public int? ProviderServiceId { get; set; }
       

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual ChildProfileAuthorizationEntity ChildProfileAuthorization { get; set; }
        public virtual ProviderBillingCodeEntity ProviderBillingCode { get; set; }
        public virtual UnitTypeEntity UnitType { get; set; }
        public virtual ProviderServiceEntity ProviderService { get; set; }
        public virtual ICollection<AppointmentEntity> Appointments { get; set; }
    }
}