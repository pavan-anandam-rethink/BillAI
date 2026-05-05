namespace BillingService.Domain.Models.Funders
{
    public class FunderServiceLineModel
    {
        public int MappingId { get; set; }
        public int ServiceId { get; set; }
        public string Name { get; set; }
        public string Sequence { get; set; }
        public int? BillingProviderOptionId { get; set; }
    }
}