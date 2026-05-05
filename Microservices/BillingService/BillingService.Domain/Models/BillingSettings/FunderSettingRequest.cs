using BillingService.Domain.Models.Audit;

namespace BillingService.Domain.Models.BillingSettings;

public sealed class FunderSettingRequest : BaseAuditRequest<FunderSettingsRequest>
{
    public int? Id { get; set; }
    public int FunderId { get; set; }
    public int ClaimFilingIndicatorId { get; set; }
    public bool IncludeTaxonomyCode { get; set; }
    public int AccountInfoId { get; set; }
    public string FunderName { get; set; }
}