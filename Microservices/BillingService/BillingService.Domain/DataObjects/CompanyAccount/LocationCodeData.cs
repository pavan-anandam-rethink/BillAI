namespace BillingService.Domain.DataObjects.CompanyAccount
{
    public class LocationCodeData
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
        public bool? IsActive { get; set; } = true;
    }
}
