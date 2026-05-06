using Rethink.Services.Domain.Services.RethinkServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BillingService.Web.Middlewares
{
    /// <summary>
    /// Binds JWT billing session + account to <see cref="IRethinkBillingRequestContext"/> for BH master data Redis cache keys.
    /// </summary>
    public sealed class BillingMasterDataRequestMiddleware
    {
        private static readonly JwtSecurityTokenHandler _tokenHandler = new();
        private readonly RequestDelegate _next;
        private readonly ILogger<BillingMasterDataRequestMiddleware>? _logger;

        public BillingMasterDataRequestMiddleware(RequestDelegate next, ILogger<BillingMasterDataRequestMiddleware>? logger = null)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IRethinkBillingRequestContext billingContext)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var sessionKey = context.User.FindFirstValue("BillingSessionKey");
                var accountClaim = context.User.FindFirstValue("AccountInfoId");
                if (!string.IsNullOrEmpty(sessionKey) && int.TryParse(accountClaim, out var accountId) && accountId > 0)
                {
                    billingContext.SessionKey = sessionKey;
                    billingContext.AccountInfoId = accountId;
                }
            }
            else if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var token = authHeader.ToString();
                if (token.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
                {
                    token = token["Bearer ".Length..].Trim();
                    try
                    {
                        if (_tokenHandler.CanReadToken(token))
                        {
                            var jwt = _tokenHandler.ReadJwtToken(token);
                            var sessionKey = jwt.Claims.FirstOrDefault(c => c.Type == "BillingSessionKey")?.Value;
                            var accountClaim = jwt.Claims.FirstOrDefault(c => c.Type == "AccountInfoId")?.Value;
                            if (!string.IsNullOrEmpty(sessionKey) && int.TryParse(accountClaim, out var accountId) && accountId > 0)
                            {
                                billingContext.SessionKey = sessionKey;
                                billingContext.AccountInfoId = accountId;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        _logger?.LogDebug(ex, "Could not read billing session from JWT for master data context");
                    }
                }
            }

            await _next(context);
        }
    }
}
