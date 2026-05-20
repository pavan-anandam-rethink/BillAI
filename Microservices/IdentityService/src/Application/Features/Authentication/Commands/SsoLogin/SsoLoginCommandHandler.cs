using IdentityService.Application.DTOs;
using IdentityService.Application.Exceptions;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Events;
using IdentityService.Domain.Interfaces;
using IdentityService.Domain.ValueObjects;
using IdentityService.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IdentityService.Application.Features.Authentication.Commands.SsoLogin;

public class SsoLoginCommandHandler(
    ITokenService tokenService,
    ITokenValidationApiService tokenValidationApi,
    IUserProfileRepository userProfileRepository,
    ICacheService cacheService,
    IAccountService accountService,
    ISessionPrewarmService sessionPrewarm,
    IConfiguration config,
    ICurrentUserService currentUserService,
    IMediator mediator,
    ILogger<SsoLoginCommandHandler> logger) : IRequestHandler<SsoLoginCommand, AuthenticatedResponseDto>
{
    private static readonly JsonSerializerSettings JsonSettings = new() { NullValueHandling = NullValueHandling.Ignore };

    public async Task<AuthenticatedResponseDto> Handle(SsoLoginCommand request, CancellationToken cancellationToken)
    {
        // Validate rethink token via external API
        var encryptedData = await tokenValidationApi.ValidateRethinkTokenAsync(request.RethinkToken, cancellationToken);

        if (string.IsNullOrEmpty(encryptedData))
            throw new UnauthorizedException("Invalid Rethink token");

        // Decrypt and deserialize auth request
        var tokenValidationKey = config["TokenValidationApiKey"]!;
        var decryptedJson = tokenService.DecryptString(tokenValidationKey, encryptedData);
        var authRequest = JsonConvert.DeserializeObject<AuthenticateRequestDto>(decryptedJson, JsonSettings);

        if (authRequest == null)
            throw new UnauthorizedException("Invalid Rethink token");

        // Resolve impersonation user profile if needed
        if (!string.IsNullOrEmpty(authRequest.ImpersonationUserObjectId))
        {
            try
            {
                var userProfile = await FindUserProfileByMsalIdAsync(authRequest.ImpersonationUserObjectId, cancellationToken);
                if (userProfile != null)
                {
                    authRequest.ImpersonationUserName = userProfile.FullName ?? string.Empty;
                    authRequest.ImpersonationUserEmail = userProfile.Email ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to lookup impersonation user profile for {ObjectId}",
                    authRequest.ImpersonationUserObjectId);
            }
        }

        // Generate tokens
        var jwtSettings = GetJwtSettings();
        var jwtToken = await tokenService.GenerateAccessTokenAsync(jwtSettings, authRequest);
        var refreshToken = tokenService.GenerateRefreshToken();

        // Fire-and-forget session prewarm
        if (int.TryParse(authRequest.AccountInfoId, out var accountId) && accountId > 0
            && !string.IsNullOrEmpty(authRequest.BillingSessionKey))
        {
            var prewarmTimeoutSeconds = config.GetValue("RethinkMasterDataSession:PrewarmTimeoutSeconds", 15);
            _ = FireAndForgetPrewarmAsync(accountId, authRequest.BillingSessionKey, prewarmTimeoutSeconds);
        }

        // Publish domain event
        await mediator.Publish(new UserLoggedInEvent(
            authRequest.MemberId,
            authRequest.AccountInfoId,
            currentUserService.IpAddress ?? "unknown",
            DateTime.UtcNow), cancellationToken);

        return new AuthenticatedResponseDto
        {
            Token = jwtToken,
            RefreshToken = refreshToken,
            BillingSessionKey = authRequest.BillingSessionKey
        };
    }

    private async Task<Domain.Entities.UserProfile?> FindUserProfileByMsalIdAsync(string msalObjectId, CancellationToken cancellationToken)
    {
        var decodedId = DecodeBase64(msalObjectId);

        // Check cache first
        var cacheKey = $"{CacheKeys.MsalMappingPrefix}";
        var msalToUserIds = await cacheService.GetOrCreateAsync(
            cacheKey,
            () => Task.FromResult(new Dictionary<string, string>()),
            TimeSpan.FromDays(1),
            cancellationToken);

        if (msalToUserIds.TryGetValue(decodedId, out var userId))
        {
            return await userProfileRepository.GetByIdAsync(userId, cancellationToken);
        }

        var profile = await userProfileRepository.FindOneAsync(x => x.MsalObjectId == decodedId, cancellationToken);
        if (profile != null)
        {
            msalToUserIds.TryAdd(profile.MsalObjectId, profile.Id);
            await cacheService.SetAsync(cacheKey, msalToUserIds, TimeSpan.FromDays(1), cancellationToken);
        }

        return profile;
    }

    private async Task FireAndForgetPrewarmAsync(int accountId, string billingSessionKey, int timeoutSeconds)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var warmTask = sessionPrewarm.WarmAsync(accountId, billingSessionKey, cts.Token);
            var completed = await Task.WhenAny(warmTask, Task.Delay(Timeout.Infinite, cts.Token));
            if (completed != warmTask)
            {
                logger.LogWarning("Session prewarm timed out for account {AccountId}", accountId);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Session prewarm failed for account {AccountId}", accountId);
        }
    }

    private JwtSettings GetJwtSettings() => new()
    {
        Key = config["Jwt:Key"]!,
        Issuer = config["Jwt:Issuer"]!,
        Audience = config["Jwt:Audience"] ?? config["Jwt:Issuer"]!
    };

    private static string DecodeBase64(string base64String)
    {
        if (string.IsNullOrEmpty(base64String)) return string.Empty;
        try
        {
            var bytes = Convert.FromBase64String(base64String);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return base64String;
        }
    }
}
