using System.Text;
using BillingService.App.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BillingService.App.LegacyAdapters;

public sealed class HttpLegacyBillingGateway : ILegacyBillingGateway
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LegacyBillingOptions _options;
    private readonly ILogger<HttpLegacyBillingGateway> _logger;

    public HttpLegacyBillingGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<LegacyBillingOptions> options,
        ILogger<HttpLegacyBillingGateway> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LegacyGatewayResponse> ForwardJsonAsync(
        string relativePath,
        string jsonPayload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("legacy-billing");
        using var request = new HttpRequestMessage(HttpMethod.Post, relativePath)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };

        foreach (var kvp in headers)
        {
            request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
        }

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Legacy endpoint {Path} returned {StatusCode}",
                relativePath,
                (int)response.StatusCode);
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/json";
        return new LegacyGatewayResponse((int)response.StatusCode, body, contentType);
    }
}

