using IdentityService.Application.Interfaces;
using IdentityService.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace IdentityService.Infrastructure.Identity;

public class JwtMiddleware(RequestDelegate next, IConfiguration config)
{
    private const string AuthorizationHeader = "Authorization";

    public async Task Invoke(HttpContext context, ITokenService tokenService)
    {
        string errorMessage = string.Empty;
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            var isAllowAnonymous = endpoint.Metadata.Any(x => x.GetType() == typeof(AllowAnonymousAttribute));

            if (!isAllowAnonymous)
            {
                bool jwtTokenExists = context.Request.Headers.TryGetValue(AuthorizationHeader, out var authHeader);
                if (jwtTokenExists)
                {
                    string token = authHeader.ToString().Replace("Bearer ", "");
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

    private string ValidateJwtToken(StringValues jwtToken, ITokenService tokenService)
    {
        if (jwtToken.Count > 1)
            return $"Request returned multiple headers for {AuthorizationHeader}";

        if (string.IsNullOrWhiteSpace(jwtToken))
            return $"{AuthorizationHeader} is null or whitespace";

        var jwtSettings = new JwtSettings
        {
            Key = config["Jwt:Key"]!,
            Issuer = config["Jwt:Issuer"]!,
            Audience = config["Jwt:Audience"] ?? config["Jwt:Issuer"]!
        };

        if (!tokenService.IsTokenValid(jwtSettings, jwtToken!))
            return "JWT token invalid";

        return string.Empty;
    }
}
