using Authentication.Interfaces;
using Authentication.Models;
using Microsoft.IdentityModel.Tokens;
using Rethink.Services.Common.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Authentication.Services
{
    public class TokenService : ITokenService
    {
        private const double EXPIRY_DURATION_MINUTES = 5;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        public const string BillingView = "BillingView";
        public const string BillingPostPayments = "BillingPostPayments";
        public const string BillingReopenEncounter = "BillingReopenEncounter";
        public const string BillingCloseEncounters = "BillingCloseEncounters";
        public const string BillingClientHistory = "BillingClientHistory";

        // OSB permission whitelist (case-insensitive)
        private static readonly HashSet<string> OsbPermissionWhitelist = new(StringComparer.OrdinalIgnoreCase)
        {
            BillingView,
            BillingPostPayments,
            BillingReopenEncounter,
            BillingCloseEncounters,
            BillingClientHistory
        };

        public TokenService(IRethinkMasterDataMicroServices rethinkMasterData)
        {
            _rethinkServices = rethinkMasterData;
        }

        public async Task<string> GenerateAccessToken(string key, string issuer, AuthenticateRequest authRequest)
        {
            if (authRequest == null) return string.Empty;

            bool osbEnabled = false;
            var accountDetail = string.Empty;


            if (!string.IsNullOrWhiteSpace(authRequest.AccountInfoId))
            {
                try
                {
                    var accountInfo = await _rethinkServices.GetAccountReturningEntityAsync(int.Parse(authRequest.AccountInfoId), false);
                    osbEnabled = accountInfo.subscriptionFeatures != null
                                 && accountInfo.subscriptionFeatures.ContainsKey("showOSBFlag")
                                 && (bool)accountInfo.subscriptionFeatures["showOSBFlag"];
                    var accountId = accountInfo?.Id ?? 0;
                    var accountName = accountInfo?.Name;
                    accountDetail = $"{accountName} ({accountId})";

                }
                catch (Exception ex)
                {
                    osbEnabled = false;
                }
            }

            if (string.IsNullOrWhiteSpace(authRequest.BillingSessionKey)
                && !string.IsNullOrWhiteSpace(authRequest.AccountInfoId))
            {
                authRequest.BillingSessionKey = Guid.NewGuid().ToString("N");
            }

            var token = GenerateJWTToken(key, issuer, authRequest, osbEnabled, accountDetail);

            return token;
        }

        public bool IsTokenValid(string key, string issuer, string audience, string token)
        {
            var mySecret = Encoding.UTF8.GetBytes(key);
            var mySecurityKey = new SymmetricSecurityKey(mySecret);
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ValidateToken(token,
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = mySecurityKey,
                    }, out SecurityToken validatedToken);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string key, string issuer, string audience, string token)
        {
            var mySecret = Encoding.UTF8.GetBytes(key);
            var mySecurityKey = new SymmetricSecurityKey(mySecret);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = mySecurityKey,
                ValidateLifetime = false
            };
            return tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
        }

        private static string GenerateJWTToken(string key, string issuer, AuthenticateRequest authRequest, bool osbEnabled, string accountDetail)
        {
            var claims = new List<Claim>
            {
                new("AccountInfoId", authRequest.AccountInfoId),
                new("MemberId", authRequest.MemberId),
                new("MemberName", authRequest.MemberName),
                new("MemberRole", authRequest.MemberRole),
                new("OsbEnabled", osbEnabled.ToString().ToLowerInvariant()),
                new("ImpersonatedUser", authRequest.ImpersonationUserObjectId.ToString()),
                new("ImpersonationUserName", authRequest.ImpersonationUserName),
                new("ImpersonationUserEmail", authRequest.ImpersonationUserEmail),
                new("AccountDetail", accountDetail),
                new("BillingSessionKey", authRequest.BillingSessionKey ?? string.Empty)
            };

            IEnumerable<KeyValuePair<string, bool>> permissionSource = authRequest.Permissions ?? new Dictionary<string, bool>();

            if (osbEnabled && string.IsNullOrEmpty(authRequest.ImpersonationUserObjectId))
            {
                permissionSource = permissionSource
                    .Where(kvp => kvp.Value)
                    .Where(kvp => OsbPermissionWhitelist.Contains(kvp.Key));
            }
            else
            {
                permissionSource = permissionSource
                    .Where(kvp => kvp.Value)
                    .Select(kvp => new KeyValuePair<string, bool>(kvp.Key.ToLowerInvariant(), kvp.Value));
            }

            foreach (var permission in permissionSource)
            {
                claims.Add(new Claim("Permissions", permission.Key.ToLowerInvariant()));
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
            var tokenDescriptor = new JwtSecurityToken(issuer, issuer, claims,
                expires: DateTime.Now.AddMinutes(EXPIRY_DURATION_MINUTES), signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        public string DecryptString(string key, string cipherText)
        {
            byte[] buffer = Convert.FromBase64String(cipherText);

            using Aes aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Encoding.UTF8.GetBytes(key);
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using MemoryStream memoryStream = new(buffer);
            using CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read);
            using StreamReader streamReader = new(cryptoStream);
            return streamReader.ReadToEnd();
        }
    }
}
