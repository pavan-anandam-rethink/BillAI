using Rethink.Services.Common.Models;
using System;

namespace BillingService.Domain.Models.Clients
{
    public class ProviderLocationData
    {
        public int Id { get; set; }
        public int AccountInfoId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Fax { get; set; }
        public bool IsMainLocation { get; set; }
        public bool IsBillingLocation { get; set; }
        public string AgencyName { get; set; }
        public string FederalTaxId { get; set; }
        public string NpiNumber { get; set; }
        public string TaxonomyCode { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public string ProviderCommercialNumber { get; set; }
        public string StateLicenseNumber { get; set; }
        public string LocationNumber { get; set; }
        public ClientAddress Address { get; set; }
    }
}
