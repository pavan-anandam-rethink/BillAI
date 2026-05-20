using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace IdentityService.Infrastructure.Services;

public class TokenValidationApiService(
    IHttpClientFactory httpClientFactory,
    ILogger<TokenValidationApiService> logger) : ITokenValidationApiService
{
    public async Task<string?> ValidateRethinkTokenAsync(string rethinkToken, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("tokenValidation");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rethinkToken);

        var response = await client.GetAsync("core/api/integrations/billing/GetBillingStaffData", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<string>(body);
        }

        logger.LogWarning("Token validation API returned {StatusCode}", response.StatusCode);
        return null;
    }
}
