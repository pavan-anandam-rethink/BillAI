using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.ValueObjects;
using IdentityService.Shared.Constants;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IdentityService.Infrastructure.Services;

public class TokenService : ITokenService
{
    private static readonly HashSet<string> OsbPermissionWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        AuthConstants.OsbPermissions.BillingView,
        AuthConstants.OsbPermissions.BillingPostPayments,
        AuthConstants.OsbPermissions.BillingReopenEncounter,
        AuthConstants.OsbPermissions.BillingCloseEncounters,
        AuthConstants.OsbPermissions.BillingClientHistory
    };

    private readonly IAccountService _accountService;
    private readonly ICacheService _cacheService;

    public TokenService(IAccountService accountService, ICacheService cacheService)
    {
        _accountService = accountService;
        _cacheService = cacheService;
    }

    public async Task<string> GenerateAccessTokenAsync(JwtSettings settings, AuthenticateRequestDto authRequest)
    {
        if (authRequest == null) return string.Empty;

        bool osbEnabled = false;
        var accountDetail = string.Empty;

        if (!string.IsNullOrWhiteSpace(authRequest.AccountInfoId))
        {
            try
            {
                var accountId = int.Parse(authRequest.AccountInfoId);
                var accountInfo = await _cacheService.GetOrCreateAsync(
                    $"{CacheKeys.AccountInfoPrefix}_{accountId}",
                    () => _accountService.GetAccountInfoAsync(accountId),
                    TimeSpan.FromMinutes(5));

                if (accountInfo != null)
                {
                    osbEnabled = accountInfo.OsbEnabled;
                    accountDetail = $"{accountInfo.Name} ({accountInfo.Id})";
                }
            }
            catch (Exception)
            {
                osbEnabled = false;
            }
        }

        if (string.IsNullOrWhiteSpace(authRequest.BillingSessionKey)
            && !string.IsNullOrWhiteSpace(authRequest.AccountInfoId))
        {
            authRequest.BillingSessionKey = Guid.NewGuid().ToString("N");
        }

        return GenerateJwtToken(settings, authRequest, osbEnabled, accountDetail);
    }

    public bool IsTokenValid(JwtSettings settings, string token)
    {
        var mySecret = Encoding.UTF8.GetBytes(settings.Key);
        var mySecurityKey = new SymmetricSecurityKey(mySecret);
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = settings.Issuer,
                ValidAudience = settings.Audience,
                IssuerSigningKey = mySecurityKey,
            }, out _);
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

    public ClaimsPrincipal GetPrincipalFromExpiredToken(JwtSettings settings, string token)
    {
        var mySecret = Encoding.UTF8.GetBytes(settings.Key);
        var mySecurityKey = new SymmetricSecurityKey(mySecret);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = mySecurityKey,
            ValidateLifetime = false
        };
        return tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
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

    private static string GenerateJwtToken(JwtSettings settings, AuthenticateRequestDto authRequest, bool osbEnabled, string accountDetail)
    {
        var claims = new List<Claim>
        {
            new(AuthConstants.ClaimTypes.AccountInfoId, authRequest.AccountInfoId ?? string.Empty),
            new(AuthConstants.ClaimTypes.MemberId, authRequest.MemberId ?? string.Empty),
            new(AuthConstants.ClaimTypes.MemberName, authRequest.MemberName ?? string.Empty),
            new(AuthConstants.ClaimTypes.MemberRole, authRequest.MemberRole ?? string.Empty),
            new(AuthConstants.ClaimTypes.OsbEnabled, osbEnabled.ToString().ToLowerInvariant()),
            new(AuthConstants.ClaimTypes.ImpersonatedUser, authRequest.ImpersonationUserObjectId ?? string.Empty),
            new(AuthConstants.ClaimTypes.ImpersonationUserName, authRequest.ImpersonationUserName ?? string.Empty),
            new(AuthConstants.ClaimTypes.ImpersonationUserEmail, authRequest.ImpersonationUserEmail ?? string.Empty),
            new(AuthConstants.ClaimTypes.AccountDetail, accountDetail ?? string.Empty),
            new(AuthConstants.ClaimTypes.BillingSessionKey, authRequest.BillingSessionKey ?? string.Empty)
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
            claims.Add(new Claim(AuthConstants.ClaimTypes.Permissions, permission.Key.ToLowerInvariant()));
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
        var tokenDescriptor = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            expires: DateTime.Now.AddMinutes(settings.ExpiryDurationMinutes > 0 ? settings.ExpiryDurationMinutes : AuthConstants.JwtExpiryDurationMinutes),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}
