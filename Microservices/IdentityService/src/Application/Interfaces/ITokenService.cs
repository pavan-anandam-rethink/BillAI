using IdentityService.Application.DTOs;
using IdentityService.Domain.ValueObjects;
using System.Security.Claims;

namespace IdentityService.Application.Interfaces;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(JwtSettings settings, AuthenticateRequestDto authRequest);
    string GenerateRefreshToken();
    bool IsTokenValid(JwtSettings settings, string token);
    ClaimsPrincipal GetPrincipalFromExpiredToken(JwtSettings settings, string token);
    string DecryptString(string key, string cipherText);
}
