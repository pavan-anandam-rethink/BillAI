using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BillingService.App.API.Security;

public sealed class HeaderPassthroughAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public HeaderPassthroughAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var hasJwt = Request.Headers.ContainsKey("Authorization");
        var hasApiKey = Request.Headers.ContainsKey("XApiKey");

        if (!hasJwt && !hasApiKey)
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization or XApiKey header."));
        }

        var claims = new List<Claim>
        {
            new("auth_mode", hasApiKey ? "apikey" : "bearer")
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

