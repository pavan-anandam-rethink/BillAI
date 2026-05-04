namespace BillingService.Domain.Models
{
    public class UpdateChargeModifiersModel : UserInfo
    {
        public int Id { get; set; }
        public string Modifier1 { get; set; }
        public bool? IncludeOnClaimMod1 { get; set; }
        public string Modifier2 { get; set; }
        public bool? IncludeOnClaimMod2 { get; set; }
        public string Modifier3 { get; set; }
        public bool? IncludeOnClaimMod3 { get; set; }
        public string Modifier4 { get; set; }
        public bool? IncludeOnClaimMod4 { get; set; }
    }
}
