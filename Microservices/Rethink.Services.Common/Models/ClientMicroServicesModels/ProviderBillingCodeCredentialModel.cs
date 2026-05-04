using Microsoft.EntityFrameworkCore;

namespace Rethink.Services.Common.Models
{
    [Owned]
    public class ProviderBillingCodeCredentialModel
    {
        public int providerBillingCodeId { get; set; }
        public int staffCertificationLuId { get; set; }
        public decimal contractRate { get; set; }
        public bool isPrimary { get; set; }
        public string? modifier1 { get; set; }
        public string? modifier2 { get; set; }
        public string? modifier2Name { get; set; }
        public string? modifier3 { get; set; }
        public string? modifier4 { get; set; }
        public int staffCredentialId { get; set; }
        public int id { get; set; }
        public BillingCodeData ProviderBillingCode { get; set; }
        public MetaData metaData { get; set; }
    }
}
