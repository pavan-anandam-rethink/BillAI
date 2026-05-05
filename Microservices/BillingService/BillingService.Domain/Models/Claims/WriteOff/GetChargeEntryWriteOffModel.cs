namespace BillingService.Domain.Models.Claims.WriteOff
{
    public class GetChargeEntryWriteOffModel : UserInfo
    {
        public int Id { get; set; }
        public bool IsServiceLineId { get; set; }
    }
}
