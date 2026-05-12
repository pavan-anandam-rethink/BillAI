using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ClearingHouse.SharedKernel.Infrastructure.Security;

/// <summary>
/// Provides Azure Managed Identity token acquisition for service-to-service authentication.
/// </summary>
public sealed class ManagedIdentityTokenProvider : IDisposable
{
    private readonly ILogger<ManagedIdentityTokenProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private const string ImdsEndpoint = "http://169.254.169.254/metadata/identity/oauth2/token";
    private const string ApiVersion = "2019-08-01";

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedIdentityTokenProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="httpClient">The HTTP client for token requests.</param>
    public ManagedIdentityTokenProvider(
        ILogger<ManagedIdentityTokenProvider> logger,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Acquires an access token for the specified resource.
    /// </summary>
    /// <param name="resource">The target resource URI (e.g., "https://storage.azure.com/").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token string.</returns>
    public async Task<string> GetAccessTokenAsync(string resource, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);

        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
        {
            return _cachedToken;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring the lock
            if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
            {
                return _cachedToken;
            }

            var token = await RequestTokenAsync(resource, cancellationToken);
            return token;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Creates an authentication header value for the specified resource.
    /// </summary>
    /// <param name="resource">The target resource URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An authentication header with a Bearer token.</returns>
    public async Task<AuthenticationHeaderValue> GetAuthenticationHeaderAsync(
        string resource,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(resource, cancellationToken);
        return new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Disposes the semaphore used for thread-safe token caching.
    /// </summary>
    public void Dispose()
    {
        _semaphore.Dispose();
    }

    private async Task<string> RequestTokenAsync(string resource, CancellationToken cancellationToken)
    {
        var requestUri = $"{ImdsEndpoint}?api-version={ApiVersion}&resource={Uri.EscapeDataString(resource)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("Metadata", "true");

        _logger.LogDebug("Requesting managed identity token for resource: {Resource}", resource);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        var accessToken = root.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Access token not found in IMDS response.");

        if (root.TryGetProperty("expires_on", out var expiresOn))
        {
            var expiresOnUnix = long.Parse(expiresOn.GetString()!);
            _tokenExpiry = DateTimeOffset.FromUnixTimeSeconds(expiresOnUnix).UtcDateTime;
        }

        _cachedToken = accessToken;

        _logger.LogDebug(
            "Acquired managed identity token for resource: {Resource}, expires: {Expiry}",
            resource,
            _tokenExpiry);

        return accessToken;
    }
}
