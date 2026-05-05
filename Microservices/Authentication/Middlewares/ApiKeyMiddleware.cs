using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Rethink.Services.Domain.Interfaces;

namespace Authentication.Middlewares
{
    public class ApiKeyMiddleware(RequestDelegate next, IConfiguration config, IKeyVaultProviderService keyVaultProviderService)
    {
        private const string APIKEY = "XApiKey";

        public async Task Invoke(HttpContext context)
        {
            string errorMessage = string.Empty;
            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                var isAllowAnonymous = endpoint?.Metadata.Any(x => x.GetType() == typeof(AllowAnonymousAttribute)) ?? false;
                var headers = context.Request.Headers;

                if (!isAllowAnonymous)
                {
                    bool apiKeyExists = headers.TryGetValue(APIKEY, out var appKey);
                    if (apiKeyExists)
                    {
                        errorMessage = await ValidateApiKeyAsync(appKey).ConfigureAwait(false);
                    }
                    else
                    {
                        errorMessage = "API Key was not provided ";
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

        private async Task<string> ValidateApiKeyAsync(StringValues appKey)
        {
            string errorMessage = string.Empty;
            string apiKey = await keyVaultProviderService.GetSecretAsync(config["XApiKey"]).ConfigureAwait(false);

            if (appKey.Count > 1)
            {
                errorMessage = $"Request returned multiple headers for {APIKEY}";
            }
            else if (string.IsNullOrWhiteSpace(appKey))
            {
                errorMessage = $"{APIKEY} is null or whitespace";
            }
            else if (!appKey.Equals(apiKey))
            {
                errorMessage = $"{APIKEY} is not a valid";
            }
            return errorMessage;
        }
    }
}
