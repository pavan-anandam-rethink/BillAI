using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ClientProviderLocationsModel
    {
        public int total { get; set; }
        public List<ProviderLocations> data { get; set; }
    }
    [Owned]
    public class ProviderLocations
    {
        public int accountId { get; set; }
        public string name { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string? website { get; set; }
        public int addressId { get; set; }
        public bool isMainLocation { get; set; }
        public string fax { get; set; }
        public bool isBillingLocation { get; set; }
        public string? agencyName { get; set; }
        public string? federalTaxId { get; set; }
        public string? npiNumber { get; set; }
        public string? taxonomyCode { get; set; }
        public DateTime? effectiveDate { get; set; }
        public string? providerCommercialNumber { get; set; }
        public string? stateLicenseNumber { get; set; }
        public string? locationNumber { get; set; }
        public ProviderLocationAddress address { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }
    }
    public class ProviderLocationAddress
    {
        public string street1 { get; set; }
        public string street2 { get; set; }
        public string city { get; set; }
        public int stateId { get; set; }
        public string zip { get; set; }
        public int countryId { get; set; }
        public string town { get; set; }
        public int id { get; set; }
        public MetaData metaData { get; set; }

    }

    public class EraLocationCheckModel
    {
        public int id { get; set; }
        public int accountId { get; set; }
    }
}
