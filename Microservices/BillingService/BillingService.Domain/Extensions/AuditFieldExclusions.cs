using Rethink.Services.Common.Entities.Billing;

namespace BillingService.Domain.Extensions;

public static class AuditExcludedFields
{
    public static readonly string[] FunderSettings =
    {
        nameof(FunderSettingsEntity.AccountInfoId),
        nameof(FunderSettingsEntity.FunderId)
    };
}