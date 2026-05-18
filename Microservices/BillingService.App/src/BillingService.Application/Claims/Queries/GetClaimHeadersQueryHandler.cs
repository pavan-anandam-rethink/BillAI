using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BillingService.App.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BillingService.App.Application.Claims.Queries;

public sealed class GetClaimHeadersQueryHandler : IRequestHandler<GetClaimHeadersQuery, LegacyGatewayResponse>
{
    private readonly ILegacyBillingGateway _legacyBillingGateway;
    private readonly ICacheStore _cacheStore;
    private readonly ILogger<GetClaimHeadersQueryHandler> _logger;

    public GetClaimHeadersQueryHandler(
        ILegacyBillingGateway legacyBillingGateway,
        ICacheStore cacheStore,
        ILogger<GetClaimHeadersQueryHandler> logger)
    {
        _legacyBillingGateway = legacyBillingGateway;
        _cacheStore = cacheStore;
        _logger = logger;
    }

    public async Task<LegacyGatewayResponse> Handle(GetClaimHeadersQuery request, CancellationToken cancellationToken)
    {
        var key = BuildKey(request.JsonPayload, request.ForwardHeaders);
        if (request.UseCache)
        {
            var cached = await _cacheStore.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                var cachedResponse = JsonSerializer.Deserialize<CachedGatewayResponse>(cached);
                if (cachedResponse is not null)
                {
                    return new LegacyGatewayResponse(
                        cachedResponse.StatusCode,
                        cachedResponse.Content,
                        cachedResponse.ContentType);
                }
            }
        }

        var response = await _legacyBillingGateway
            .ForwardJsonAsync("Claim/GetClaimHeaders", request.JsonPayload, request.ForwardHeaders, cancellationToken)
            .ConfigureAwait(false);

        if (request.UseCache && response.StatusCode == 200)
        {
            var cachedPayload = JsonSerializer.Serialize(new CachedGatewayResponse(
                response.StatusCode,
                response.Content,
                response.ContentType));

            await _cacheStore
                .SetStringAsync(key, cachedPayload, TimeSpan.FromSeconds(30), cancellationToken)
                .ConfigureAwait(false);
        }

        _logger.LogInformation("Claim headers response generated with cache key {CacheKey}", key);
        return response;
    }

    private static string BuildKey(string payload, IReadOnlyDictionary<string, string> headers)
    {
        var auth = headers.TryGetValue("Authorization", out var authHeader) ? authHeader : string.Empty;
        var apiKey = headers.TryGetValue("XApiKey", out var apiKeyHeader) ? apiKeyHeader : string.Empty;
        var identityFingerprint = $"{auth}|{apiKey}";

        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        var identityHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identityFingerprint)));
        return $"billing:claim:getheaders:{identityHash}:{payloadHash}";
    }

    private sealed record CachedGatewayResponse(int StatusCode, string Content, string ContentType);
}

