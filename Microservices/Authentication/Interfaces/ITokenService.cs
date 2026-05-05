using Authentication.Models;
using System.Security.Claims;

namespace Authentication.Interfaces
{
    public interface ITokenService
    {
        bool IsTokenValid(string key, string issuer, string audience, string token);
        Task<string> GenerateAccessToken(string key, string issuer, AuthenticateRequest authRequest);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string key, string issuer, string audience, string token);
        public string DecryptString(string key, string cipherText);
    }
}
