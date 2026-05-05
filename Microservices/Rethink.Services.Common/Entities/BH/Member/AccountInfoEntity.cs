using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.BH.Billing;
using Rethink.Services.Common.Entities.BH.ChildProfile;
using Rethink.Services.Common.Entities.BH.Company;
using Rethink.Services.Common.Entities.BH.Service;

namespace Rethink.Services.Common.Entities.BH.Member
{
    public class AccountInfoEntity : BasePersistEntity, IAuditedEntity
    {
        public int? ClearingHouseId { get; set; }

        public string BillingFirstname { get; set; }
        public string BillingLastname { get; set; }
        public string BillingAddress1 { get; set; }
        public string BillingAddress2 { get; set; }
        public string BillingAddress3 { get; set; }
        public string BillingCity { get; set; }
        public string BillingZip { get; set; }
        public int? BillingStateId { get; set; }
        public int BillingCountryId { get; set; }
        public int? CreditCardTypeId { get; set; }
        public string CreditCardNumber { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string BraintreeKey { get; set; }

        public int? AccountType { get; set; }
        public int? ParentId { get; set; }
        public string AccountOrganizationName { get; set; }
        public int? AccountOrganizationTypeId { get; set; }
        public string AccountAddress1 { get; set; }
        public string AccountAddress2 { get; set; }
        public string AccountAddress3 { get; set; }
        public string AccountCity { get; set; }
        public int? AccountStateId { get; set; }
        public int? AccountCountryId { get; set; }
        public string AccountZip { get; set; }
        public string ApiKey { get; set; }
        public bool TestAcct { get; set; }
        public string EmpBenefitkey { get; set; }
        public string EbSubscriptionType { get; set; }
        public bool? CoachingSessions { get; set; }
        public string TrainingProgramKey { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string PhoneNumber { get; set; }
        public string FaxNumber { get; set; }
        public string ProviderLogo { get; set; }
        public string FederalTaxId { get; set; }
        public string NationalProviderId { get; set; }
        public bool IsLockoutRequired { get; set; }
        public string EbWelcomeText { get; set; }
        public string EbCompanyLogo { get; set; }
        public int? EbLogoWidth { get; set; }
        public int? EbLogoHeight { get; set; }
        public string EbSignUpCode { get; set; }
        public int? LearningProcessAttributesId { get; set; }

        public bool? EnableAppointmentReminders { get; set; }
        [Column("hcAppointmentReminderTypeId")]
        public int? AppointmentReminderTypeId { get; set; }
        public int? CalendarStartHour { get; set; }
        public int? CalendarEndHour { get; set; }
        [Column("hcLocationCodeId")]
        public int? LocationCodeId { get; set; }
        public bool? IsStaffVerificationRequired { get; set; }
        public bool? IsParentVerificationRequired { get; set; }
        public bool? IsSessionNoteEnteredRequired { get; set; }

        [Column("hcTimezoneId")]
        public int? TimezoneId { get; set; }

        public string TProId { get; set; }

        public bool? ShowLogoOnRbt { get; set; }
        public string BillingProviderName { get; set; }
        public string BillingProviderEmail { get; set; }
        public string BillingProviderPhone { get; set; }
        public string BillingProviderExtension { get; set; }
        public string BillingProviderFax { get; set; }
        public bool? IsInternational { get; set; }

        public string AccountTown { get; set; }
        public string BillingTown { get; set; }
        public string BillingProviderTaxonomyCode { get; set; }


        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }


        public virtual ICollection<ClientStatusEntity> ClientStatuses { get; set; }
        public virtual ClearingHouseEntity ClearingHouse { get; set; }
        public virtual StateEntity StateLU { get; set; }
        public virtual ICollection<MemberEntity> Members { get; set; }
        //public virtual ICollection<SubscriptionEntity> Subscriptions { get; set; }
        //public virtual ICollection<FunderEntity> Funders { get; set; }
        public virtual ICollection<ProviderServiceLineEntity> ProviderServiceLines { get; set; }

        //public virtual ICollection<ClientStatusEntity> ClientStatuses { get; set; }
        //public virtual ICollection<PayOverEntity> PayOvers { get; set; }
        //public virtual ICollection<AppointmentCustomTagEntity> AppointmentCustomTags { get; set; }
        //[ForeignKey("LearningProcessAttributesId")]
        //public virtual LearningProcessAttributeEntity LearningProcessAttribute { get; set; }
        //public virtual ICollection<AppointmentReminderTemplateEntity> Templates { get; set; }

        //public virtual GlobalLockEntity GlobalLock { get; set; }
   }
}