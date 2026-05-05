using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.DataObjects
{
    [ExcludeFromCodeCoverage]
    public class AccountInfoItem
    {
        public int Id { get; set; }
        //public int? AccountType { get; set; }
        //public int? ParentId { get; set; }
        public string AccountOrganizationName { get; set; }
        //public int? AccountOrganizationTypeId { get; set; }
        //public int? AccountStateId { get; set; }
        //public int? AccountCountryId { get; set; }
        //public string EbSubscriptionType { get; set; }
        //public bool? CoachingSessions { get; set; }
        //public string Name { get; set; }
        //public string PhoneNumber { get; set; }
        //public string FederalTaxId { get; set; }
        public string NationalProviderId { get; set; }
        //public int? LearningProcessAttributesId { get; set; }
        //public int? TimezoneId { get; set; }
        //public string ProId { get; set; }
        //public bool? PreAndPostTest { get; set; }
        //public int? ClearingHouseId { get; set; }
        //public bool? IsInternational { get; set; }

        //public ICollection<PayOverData> PayOvers { get; set; }
        //public ICollection<AppointmentCustomTagData> AppointmentCustomTags { get; set; }
    }
}