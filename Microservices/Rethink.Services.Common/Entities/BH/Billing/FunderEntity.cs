using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.ChildProfile;
using Rethink.Services.Common.Entities.BH.Company;
using Rethink.Services.Common.Entities.BH.Service;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.BH;
using RethinkAutism.Data.Entities.Billing;

namespace Rethink.Services.Common.Entities.BH.Billing
{
    public class FunderEntity : BasePersistEntity, IAuditedEntity
    {
        [Column("funderName")]
        public string Name { get; set; }
        public int AccountInfoId { get; set; }
        public bool IsActive { get; set; }
        public string EmailAddress { get; set; }
        public string Fax { get; set; }
        public string Phone { get; set; }
        public int AddressId { get; set; }
        public string Description { get; set; }
        [Column("hcFunderTypeId")]
        public FunderType FunderTypeId { get; set; }
        public string VendorId { get; set; }
        public bool? BillingCombineCharges { get; set; }
        public bool ReferringProviderRequiredOnClaim { get; set; }
        public int? AppointmentDuplicateClientTimeAlertId { get; set; }
        public int? AppointmentDuplicateClientTimeServiceAlertId { get; set; }
        public int? AppointmentMissingBillingDataAlertId { get; set; }

        public int? AppointmentExceedingAuthorizationAlertId { get; set; }
        [Column("hcProviderLocationId")]
        public int? ProviderLocationId { get; set; }
        [Column("hcFunderCoverageTypeId")]
        public int? FunderCoverageTypeId { get; set; }

        [Column("KareoInsuranceCompanyId")]
        public int? KareoInsuranceCompanyId { get; set; }
        [Column("hcCombineChargeTypeId")]
        public int? CombineChargeTypeId { get; set; }
        [Column("hcBillingProviderOptionId")]
        public BillingProviderOptionType? BillingProviderOption { get; set; }
        [Column("ClearingHousePayerId")]
        public int? ClearingHousePayerId { get; set; }
        public bool? AllowOverlappingAppointments { get; set; }
        public int? AppointmentExpiredCertificationAlertId { get; set; }
        [Column("IncludeKareoSvcApptTime")]
        public bool? IncludeKareoSvcApptTime { get; set; }

        public string Note { get; set; }
        public int? ElectronicVisitVendorId { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }

        //public virtual FunderTypeLUEntity FunderType { get; set; }
        //public virtual AccountInfoEntity AccountInfo { get; set; }
        public virtual ProviderLocationEntity ProviderLocation { get; set; }
        public virtual AddressEntity Address { get; set; }
        public virtual IEnumerable<FunderPreventableDateEntity> FunderPreventableDates { get; set; }
        public virtual IEnumerable<FunderInsurancePlanEntity> FunderInsurancePlans { get; set; }
        //public virtual IEnumerable<FormFunderEntity> Forms { get; set; }
        public virtual IEnumerable<ServiceFunderEntity> ServiceFunders { get; set; }
        public virtual IEnumerable<ChildProfileAuthorizationEntity> ChildProfileAuthorizations { get; set; }

        //public virtual KareoInsuranceCompaniesEntity KareoInsuranceCompaniy { get; set; }
    }
}