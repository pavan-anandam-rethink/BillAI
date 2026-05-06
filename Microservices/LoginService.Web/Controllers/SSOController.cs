using Authentication.Interfaces;
using Authentication.Models;
using LoginService.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rethink.Services.Domain.Services.RethinkServices;
using System.Net.Http;
using System.Net.Http.Headers;

namespace LoginService.Web.Controllers
{
    [Route("[controller]/[action]")]
    [AllowAnonymous]
    public class SSOController : ControllerBase
    {
        private static readonly JsonSerializerSettings _jsonSettings = new() { NullValueHandling = NullValueHandling.Ignore };

        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;
        private readonly string _tokenValidationApi;
        private readonly string _tokenValidationKey;
        private readonly IUserProfileService _userProfileService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IRethinkMasterDataSessionPrewarm _sessionPrewarm;
        private readonly ILogger<SSOController> _logger;

        public SSOController(
            IConfiguration config,
            ITokenService tokenService,
            IUserProfileService userProfileService,
            IHttpClientFactory httpClientFactory,
            IRethinkMasterDataSessionPrewarm sessionPrewarm,
            ILogger<SSOController> logger)
        {
            _config = config;
            _tokenService = tokenService;
            _tokenValidationApi = Convert.ToString(_config.GetSection("TokenValidationApi").Value);
            _tokenValidationKey = Convert.ToString(_config.GetSection("TokenValidationApiKey").Value);
            _userProfileService = userProfileService;
            _httpClientFactory = httpClientFactory;
            _sessionPrewarm = sessionPrewarm;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SSOLogin([FromBody] Token rethinkToken)
        {
            if (string.IsNullOrWhiteSpace(rethinkToken.token))
            {
                return BadRequest(new { message = "Empty request - No valid Rethink token" });
            }

            AuthenticateRequest authRequest = null;
            var client = _httpClientFactory.CreateClient("tokenValidation");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rethinkToken.token);
            HttpResponseMessage response = await client.GetAsync("core/api/integrations/billing/GetBillingStaffData");

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var textContent = JsonConvert.DeserializeObject<string>(body);
                authRequest = JsonConvert.DeserializeObject<AuthenticateRequest>(_tokenService.DecryptString(_tokenValidationKey, textContent), _jsonSettings);
            }

            if (authRequest == null)
            {
                return Unauthorized(new { message = "Invalid Rethink token" });
            }

            // Run impersonation lookup concurrently with JWT generation preparation
            if (!string.IsNullOrEmpty(authRequest.ImpersonationUserObjectId))
            {
                try
                {
                    var userProfile = await _userProfileService.GetUserProfileByMsalObjectId(authRequest.ImpersonationUserObjectId, true);
                    if (userProfile != null)
                    {
                        authRequest.ImpersonationUserName = userProfile.FullName ?? string.Empty;
                        authRequest.ImpersonationUserEmail = userProfile.Email ?? string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to lookup impersonation user profile for {ObjectId}", authRequest.ImpersonationUserObjectId);
                }
            }

            var jwtToken = await _tokenService.GenerateAccessToken(_config["Jwt:Key"].ToString(), _config["Jwt:Issuer"].ToString(), authRequest);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Fire-and-forget session prewarm with timeout — do NOT block login response
            if (int.TryParse(authRequest.AccountInfoId, out var accountId) && accountId > 0
                && !string.IsNullOrEmpty(authRequest.BillingSessionKey))
            {
                var prewarmTimeoutSeconds = _config.GetValue("RethinkMasterDataSession:PrewarmTimeoutSeconds", 15);
                _ = FireAndForgetPrewarmAsync(accountId, authRequest.BillingSessionKey, prewarmTimeoutSeconds);
            }

            return Ok(new AuthenticatedResponse
            {
                Token = jwtToken,
                RefreshToken = refreshToken,
                BillingSessionKey = authRequest.BillingSessionKey
            });
        }

        /// <summary>
        /// Runs session prewarm in the background with a timeout so it never blocks login response.
        /// </summary>
        private async Task FireAndForgetPrewarmAsync(int accountId, string billingSessionKey, int timeoutSeconds)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var warmTask = _sessionPrewarm.WarmAsync(accountId, billingSessionKey);
                var completed = await Task.WhenAny(warmTask, Task.Delay(Timeout.Infinite, cts.Token));
                if (completed != warmTask)
                {
                    _logger.LogWarning("Session prewarm timed out for account {AccountId}", accountId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Session prewarm failed for account {AccountId}", accountId);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Refresh([FromBody] Token billingToken)
        {
            if (string.IsNullOrWhiteSpace(billingToken.token))
            {
                return BadRequest(new { message = "Empty request" });
            }
            string accessToken = billingToken.token;
            var principal = _tokenService.GetPrincipalFromExpiredToken(_config["Jwt:Key"].ToString(), _config["Jwt:Issuer"].ToString(), _config["Jwt:Issuer"].ToString(), accessToken);
            var claims = principal.Claims;

            AuthenticateRequest authRequest = new()
            {
                AccountInfoId = claims.SingleOrDefault(c => c.Type == "AccountInfoId").Value,
                MemberId = claims.SingleOrDefault(c => c.Type == "MemberId").Value,
                MemberName = claims.SingleOrDefault(c => c.Type == "MemberName").Value,
                MemberRole = claims.SingleOrDefault(c => c.Type == "MemberRole").Value,
                ImpersonationUserObjectId = claims.SingleOrDefault(c => c.Type == "ImpersonatedUser").Value,
                ImpersonationUserName= claims.SingleOrDefault(c => c.Type == "ImpersonationUserName").Value,
                ImpersonationUserEmail= claims.SingleOrDefault(c => c.Type == "ImpersonationUserEmail").Value,
                BillingSessionKey = claims.SingleOrDefault(c => c.Type == "BillingSessionKey")?.Value ?? string.Empty,
                Permissions = []
            };
            foreach (var permission in claims.Where(c => c.Type == "Permissions"))
            {
                //Do not confuse "permission.Value" with Rethink Permissions Key-Value pair
                authRequest.Permissions.Add(permission.Value.ToLower(), true);
            }

            var newJwtToken = await _tokenService.GenerateAccessToken(_config["Jwt:Key"].ToString(), _config["Jwt:Issuer"].ToString(), authRequest);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            return Ok(new AuthenticatedResponse
            {
                Token = newJwtToken,
                RefreshToken = newRefreshToken,
                BillingSessionKey = authRequest.BillingSessionKey
            });
        }

        //[HttpPost]
        //public IActionResult Invalidate(TokenApiModel tokenApiModel)
        //{
        // Not required since we are not associating a user with JWT token in DB
        //}
    }
}
