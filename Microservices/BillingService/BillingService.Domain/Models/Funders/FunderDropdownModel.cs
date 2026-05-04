namespace BillingService.Domain.Models.Funders
{
    public class FunderDropdownModel
    {
        public int Id { get; set; }
        public string FunderName { get; set; }
    }

    public class RawFunderDropdownModel
    {
        public string Id { get; set; }
        public string FunderName { get; set; }
    }
}