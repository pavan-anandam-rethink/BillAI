using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BillingService.Application.Abstractions.Caching;

public sealed class CacheKeyBuilder : ICacheKeyBuilder
{
    private const string ServicePrefix = "billing";

    public string BuildTenantKey(int accountInfoId, string area, params object?[] parts)
    {
        if (accountInfoId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(accountInfoId), "Tenant account id must be positive.");
        }

        return Join("tenant", accountInfoId.ToString(CultureInfo.InvariantCulture), area, parts);
    }

    public string BuildGlobalKey(string area, params object?[] parts)
    {
        return Join("global", "shared", area, parts);
    }

    private static string Join(string scope, string partition, string area, object?[] parts)
    {
        if (string.IsNullOrWhiteSpace(area))
        {
            throw new ArgumentException("Cache key area is required.", nameof(area));
        }

        var normalizedArea = Normalize(area);
        var normalizedParts = parts.Select(NormalizePart).ToArray();
        var suffix = normalizedParts.Length == 0
            ? "default"
            : string.Join(":", normalizedParts);

        return $"{ServicePrefix}:{{{scope}:{partition}}}:{normalizedArea}:{suffix}";
    }

    private static string NormalizePart(object? part)
    {
        if (part is null)
        {
            return "null";
        }

        return part switch
        {
            string value => Normalize(value),
            IFormattable formattable => Normalize(formattable.ToString(null, CultureInfo.InvariantCulture)),
            _ => Hash(JsonSerializer.Serialize(part))
        };
    }

    private static string Normalize(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? "empty"
            : value.Trim().ToLowerInvariant();

        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            builder.Append(char.IsLetterOrDigit(character) || character is '-' or '_' or '.'
                ? character
                : '-');
        }

        return builder.ToString();
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
