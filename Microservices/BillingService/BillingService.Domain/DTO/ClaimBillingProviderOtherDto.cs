namespace BillingService.Domain.Models.Claims
{
    public class ClaimBillingProviderOtherDto
    {
        public int ClaimId { get; set; }
        public string ProviderType { get; set; } = "Entity";
        public string FirstName { get; set; }
        public string LastNameOrFacilityName { get; set; }
        public string NPI { get; set; }
        public string TaxId { get; set; }
        public string TaxonomyCode { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string ZipExt { get; set; }
    }
}
