using Authentication.Interfaces;
using Authentication.Models;
using LoginService.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace LoginService.Web.Controllers
{
    [Route("[controller]/[action]")]
    [AllowAnonymous]
    public class SSOController : ControllerBase
    {

        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;
        private readonly string _tokenValidationApi;
        private readonly string _tokenValidationKey;
        private readonly IUserProfileService _userProfileService;

        public SSOController(IConfiguration config, ITokenService tokenService, IUserProfileService userProfileService)
        {
            _config = config;
            _tokenService = tokenService;
            _tokenValidationApi = Convert.ToString(_config.GetSection("TokenValidationApi").Value);
            _tokenValidationKey = Convert.ToString(_config.GetSection("TokenValidationApiKey").Value);
            _userProfileService = userProfileService;
        }

        [HttpPost]
        public async Task<IActionResult> SSOLogin([FromBody] Token rethinkToken)
        {
            if (string.IsNullOrWhiteSpace(rethinkToken.token))
            {
                return BadRequest(new { message = "Empty request - No valid Rethink token" });
            }

            //AuthenticateRequest authRequest = new()
            //{
            //    AccountInfoId = "18421",
            //    MemberId = "105815",
            //    MemberName = "Healthcare_08015",
            //    MemberRole = "Role 4a",
            //    Permissions = new()
            //    {
            //        { "billingview", true},
            //        { "billingedit", true},
            //        { "billingeditapprovedappointments", true},
            //        { "billingapprove", true},
            //        { "billingsubmitclaims", true},
            //        { "billingpostpayments", true},
            //        { "billingreopenencounter", true},
            //        { "billingcloseencounters", true}
            //    }
            //};
            AuthenticateRequest authRequest = null;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_tokenValidationApi);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + rethinkToken.token);
                HttpResponseMessage response = await client.GetAsync("core/api/integrations/billing/GetBillingStaffData");
                JsonSerializerSettings settings = new() { NullValueHandling = NullValueHandling.Ignore };

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var textContent = JsonConvert.DeserializeObject<string>(body);
                    authRequest = JsonConvert.DeserializeObject<AuthenticateRequest>(_tokenService.DecryptString(_tokenValidationKey, textContent), settings);
                }
            }

            if (authRequest == null)
            {
                return Unauthorized(new { message = "Invalid Rethink token" });
            }

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
                catch
                {

                }
            }

            var jwtToken = await _tokenService.GenerateAccessToken(_config["Jwt:Key"].ToString(), _config["Jwt:Issuer"].ToString(), authRequest);
            var refreshToken = _tokenService.GenerateRefreshToken();

            return Ok(new AuthenticatedResponse
            {
                Token = jwtToken,
                RefreshToken = refreshToken
            });
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
                RefreshToken = newRefreshToken
            });
        }

        //[HttpPost]
        //public IActionResult Invalidate(TokenApiModel tokenApiModel)
        //{
        // Not required since we are not associating a user with JWT token in DB
        //}
    }
}
