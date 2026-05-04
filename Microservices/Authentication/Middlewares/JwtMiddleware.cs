using Authentication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Rethink.Services.Domain.Interfaces;

namespace Authentication.Middlewares
{
    public class JwtMiddleware(RequestDelegate next, IConfiguration config, IKeyVaultProviderService keyVaultProviderService)
    {
        private ITokenService _tokenService;
        private const string AUTHORIZATION = "Authorization";
        private const string jwtKey = "Jwt:Key";
        private const string Issuer = "Jwt:Issuer";
        private const string Audience = "Jwt:Audience";

        public async Task Invoke(HttpContext context, ITokenService tokenService)
        {
            _tokenService = tokenService;
            string errorMessage = string.Empty;
            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                var isAllowAnonymous = endpoint?.Metadata.Any(x => x.GetType() == typeof(AllowAnonymousAttribute)) ?? false;
                var headers = context.Request.Headers;
                if (!isAllowAnonymous)
                {
                    bool jwtTokenExists = headers.TryGetValue(AUTHORIZATION, out var jwtToken);
                    if (jwtTokenExists)
                    {
                        string token = jwtToken.ToString().Replace("Bearer ", "");
                        errorMessage = ValidateJwtToken(token, tokenService);
                    }
                    else
                    {
                        errorMessage = "JWT token was not provided";
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

        public string ValidateJwtToken(StringValues jwtToken,ITokenService tokenService)
        {
            string errorMessage = string.Empty;

            if (jwtToken.Count > 1)
            {
                errorMessage = $"Request returned multiple headers for {AUTHORIZATION}";
            }
            else if (string.IsNullOrWhiteSpace(jwtToken))
            {
                errorMessage = $"{AUTHORIZATION} is null or whitespace";
            }
            else
            {
                // Retrive jwt token values from vault 
                var userIdSecret = keyVaultProviderService.GetSecretAsync(config["Jwt:Key"]).Result;

                if (!_tokenService.IsTokenValid(userIdSecret, config[Issuer].ToString(), config[Audience].ToString(), jwtToken))
                {
                    errorMessage = "JWT token invalid";
                }
            }
            return errorMessage;
        }
    }
}
