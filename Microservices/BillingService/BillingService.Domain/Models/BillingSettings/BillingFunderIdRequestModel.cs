namespace BillingService.Domain.Models.BillingSettings;

public sealed class BillingFunderIdRequestModel : FunderSettingsRequest
{ 
    public int Id { get; set; }
    public int AccountInfoId { get; set; }
    public int FunderId { get; set; }
    public string FunderName { get; set; }
}