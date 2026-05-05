using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Billing;
using Rethink.Services.Common.Entities.BH.Company;
using Rethink.Services.Common.Entities.BH.Member;

namespace Rethink.Services.Common.Entities.BH.Service
{
    public class ProviderBillingCodeEntity : BasePersistEntity, IAuditedEntity
    {
        public int AccountInfoId { get; set; }
        [Column("hcFunderId")]
        public int FunderId { get; set; }
        public string BillingCode { get; set; }
        public decimal? Rate { get; set; }
        [Column("hcUnitTypeId")]
        public int UnitTypeId { get; set; }
        [Column("hcProviderBillingCodeRateTypeId")]
        public int? RateTypeId { get; set; }
        [Column("hcProviderBillingCodeRoundingTypeId")]
        public int? RoundingTypeId { get; set; }
        public string BillingCode2 { get; set; }
        public decimal? Rate2 { get; set; }
        [Column("hcUnitTypeId2")]
        public int? UnitTypeId2 { get; set; }
        [Column("hcProviderBillingCodeRoundingTypeId2")]
        public int? RoundingTypeId2 { get; set; }
        [Column("hcProviderServiceId")]
        public int? ProviderServiceId { get; set; }
        public bool? RestrictStaffProviderToService { get; set; }
        public string Description { get; set; }
        [Column("hcServiceId")]
        public int ServiceId { get; set; }
        [Column("hcFrequencyTypeId")]
        public int? FrequencyTypeId { get; set; }
        public string Modifier { get; set; }
        public bool? Combined { get; set; }
        public int? BillingCodeTemplateId { get; set; }
        public int? DurationTypeId { get; set; }
        public int? Duration { get; set; }
        public DateTime? PropagatingEndDate { get; set; }
        public bool? NoAuthRequired { get; set; }
        public int? RenderingProviderTypeId { get; set; }
        public int? RenderingProviderStaffId { get; set; }
        public bool? Inactive { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual UnitTypeEntity UnitType { get; set; }
        public virtual UnitTypeEntity UnitType2 { get; set; }
        public virtual ProviderServiceEntity ProviderService { get; set; }
        public virtual FunderEntity Funder { get; set; }
        public virtual AccountInfoEntity AccountInfo { get; set; }
        public virtual MemberEntity RenderingProviderStaff { get; set; }
        public virtual List<ProviderBillingCodeCredentialEntity> ProviderBillingCodeCredentials { get; set; } =
            new List<ProviderBillingCodeCredentialEntity>();
        //public virtual List<ChildProfileAuthorizationBillingCodeEntity> 
        //    ChildProfileAuthorizationBillingCodes { get; set; } =
        //    new List<ChildProfileAuthorizationBillingCodeEntity>();
    }
}