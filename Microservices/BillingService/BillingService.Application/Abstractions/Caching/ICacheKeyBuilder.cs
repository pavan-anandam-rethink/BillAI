namespace BillingService.Application.Abstractions.Caching;

public interface ICacheKeyBuilder
{
    string BuildTenantKey(int accountInfoId, string area, params object?[] parts);

    string BuildGlobalKey(string area, params object?[] parts);
}
