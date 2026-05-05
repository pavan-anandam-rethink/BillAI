namespace BillingService.Domain.Models.Clients
{
    public class ClientAuthorizationBillingCodeSmall
    {
        public int BillingCodeId { get; set; }
        public string BillingCodeName { get; set; }
        public string BillingCodeName2 { get; set; }
        public string BillingCodeDescription { get; set; }
        public int? FrequencyTypeId { get; set; }
        public int FunderId { get; set; }
        public int? ServiceLineId { get; set; }
        public int UnitTypeId { get; set; }
        public int? UnitTypeId2 { get; set; }
        public int? ProviderServiceId { get; set; }
        public bool? Inactive { get; set; }
        public bool? NoAuthRequired { get; set; }
        public string ServiceName { get; set; }
        public decimal? Rate { get; set; }
        public decimal? Rate2 { get; set; }
        public int? RenderingProviderStaffId { get; set; }
    }
}
