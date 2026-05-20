using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace IdentityService.Infrastructure.Identity;

public class ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
{
    private const string ApiKeyHeader = "XApiKey";

    public async Task Invoke(HttpContext context)
    {
        string errorMessage = string.Empty;
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            var isAllowAnonymous = endpoint.Metadata.Any(x => x.GetType() == typeof(AllowAnonymousAttribute));

            if (!isAllowAnonymous)
            {
                bool apiKeyExists = context.Request.Headers.TryGetValue(ApiKeyHeader, out var appKey);
                if (apiKeyExists)
                {
                    errorMessage = ValidateApiKey(appKey);
                }
                else
                {
                    errorMessage = "API Key was not provided";
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync(errorMessage);
            return;
        }

        await next(context);
    }

    private string ValidateApiKey(StringValues appKey)
    {
        if (appKey.Count > 1)
            return $"Request returned multiple headers for {ApiKeyHeader}";

        if (string.IsNullOrWhiteSpace(appKey))
            return $"{ApiKeyHeader} is null or whitespace";

        var expectedKey = config["XApiKey"];
        if (!string.Equals(appKey, expectedKey, StringComparison.Ordinal))
            return $"{ApiKeyHeader} is not valid";

        return string.Empty;
    }
}
