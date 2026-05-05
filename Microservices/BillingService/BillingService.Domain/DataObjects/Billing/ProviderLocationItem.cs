using System;

namespace BillingService.Domain.DataObjects.Billing
{
    public class ProviderLocationItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AgencyName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public bool IsBillingLocation { get; set; }

        public DateTime? DateDeleted { get; set; }
    }
}
