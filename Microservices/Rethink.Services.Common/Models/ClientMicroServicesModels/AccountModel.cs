using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class AccountListModel
    {
        public int total { get; set; }
        public List<AccountModel> data { get; set; }
    }
    [Owned]
    public class AccountModel
    {
        public MetaData? metaData { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public SubscriptionModel subscription { get; set; }
        public AccountAddressModel accountAddress { get; set; }
        public BillingAddressModel billingAddress { get; set; }
        public BillingNameModel billingName { get; set; }
        public List<SubscriptionOptionsModel> accountOptions { get; set; }
        public int organizationTypeId { get; set; }
        public string emailAddress { get; set; }
        public string phoneNumber { get; set; }
        public string providerLogo { get; set; }
        public string faxNumber { get; set; }
        public string website { get; set; }
        public string federalTaxId { get; set; }
        public string nationalProviderId { get; set; }
        public int hcTimezoneId { get; set; }
        public int? clearingHouseId { get; set; }
        public bool isTestAccount { get; set; }
        public string tProId { get; set; }
        public Dictionary<string, object> subscriptionFeatures { get; set; }
    }
    [Owned]
    public class BillingNameModel
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
    }
    [Owned]
    public class SubscriptionModel
    {
        public int id { get; set; }
        public int statusId { get; set; }
        public DateTime startDate { get; set; }
        public int accountId { get; set; }
        public List<SubscriptionOptionsModel> subscriptionOptions { get; set; }
    }
    [Owned]
    public class SubscriptionOptionsModel
    {
        public string type { get; set; }
        public object value { get; set; }
    }

    [Owned]
    public class AccountAddressModel
    {
        public string address1 { get; set; }
        public string? address2 { get; set; }
        public string? address3 { get; set; }
        public string city { get; set; }
        public int stateId { get; set; }
        public int countryId { get; set; }
        public string zipCode { get; set; }
        public string? town { get; set; }
    }
    [Owned]
    public class BillingAddressModel
    {
        public string address1 { get; set; }
        public string? address2 { get; set; }
        public string? address3 { get; set; }
        public string city { get; set; }
        public int stateId { get; set; }
        public int countryId { get; set; }
        public string zipCode { get; set; }
        public string? town { get; set; }
    }
    [Owned]
    public class AccountInfoEntityModel
    {
        public int Id { get; set; }
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
        public int? AppointmentReminderTypeId { get; set; }
        public int? CalendarStartHour { get; set; }
        public int? CalendarEndHour { get; set; }
        public int? LocationCodeId { get; set; }
        public bool? IsStaffVerificationRequired { get; set; }
        public bool? IsParentVerificationRequired { get; set; }
        public bool? IsSessionNoteEnteredRequired { get; set; }
        public int? TimezoneId { get; set; }
        public string TProId { get; set; }
        //public bool? ShowLogoOnRbt { get; set; } //#DBMIGRATION
        public string BillingProviderName { get; set; }
        public string BillingProviderEmail { get; set; }
        public string BillingProviderPhone { get; set; }
        public string BillingProviderExtension { get; set; }
        public string BillingProviderFax { get; set; }
        public bool? IsInternational { get; set; }
        public string AccountTown { get; set; }
        public string BillingTown { get; set; }
        public string BillingProviderTaxonomyCode { get; set; } = "";
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }
        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public virtual ClearingHouseDataModel ClearingHouse { get; set; }

        [NotMapped]
        [JsonIgnore]
        public string tProId { get; set; }

        [NotMapped]
        public Dictionary<string, object> subscriptionFeatures { get; set; }

        [NotMapped]
        public List<SubscriptionOptionsModel> subscriptionOptions { get; set; }
    }
}
