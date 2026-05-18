using System.Security.Cryptography;
using System.Text;

namespace BillingService.Infrastructure;

public static class CacheKeyBuilder
{
    public static string Build(string environment, int accountInfoId, string entity, string discriminator)
    {
        var normalizedEnvironment = Normalize(environment, "unknown");
        var normalizedEntity = Normalize(entity, "entity");
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(discriminator ?? string.Empty)))[..16].ToLowerInvariant();

        return $"billing:{normalizedEnvironment}:{accountInfoId}:{normalizedEntity}:{digest}";
    }

    private static string Normalize(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Trim().Replace(' ', '-').ToLowerInvariant();
    }
}
