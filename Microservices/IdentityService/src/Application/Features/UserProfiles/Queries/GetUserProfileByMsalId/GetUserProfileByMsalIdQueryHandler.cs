using AutoMapper;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Interfaces;
using IdentityService.Shared.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Features.UserProfiles.Queries.GetUserProfileByMsalId;

public class GetUserProfileByMsalIdQueryHandler(
    IUserProfileRepository repository,
    ICacheService cacheService,
    IMapper mapper,
    ILogger<GetUserProfileByMsalIdQueryHandler> logger) : IRequestHandler<GetUserProfileByMsalIdQuery, UserProfileDto?>
{
    public async Task<UserProfileDto?> Handle(GetUserProfileByMsalIdQuery request, CancellationToken cancellationToken)
    {
        var msalObjectId = DecodeBase64(request.MsalObjectId);

        // Check MSAL-to-UserId cache
        var cacheKey = CacheKeys.MsalMappingPrefix;
        var msalToUserIds = await cacheService.GetOrCreateAsync(
            cacheKey,
            () => Task.FromResult(new Dictionary<string, string>()),
            TimeSpan.FromDays(1),
            cancellationToken);

        if (msalToUserIds.TryGetValue(msalObjectId, out var userId))
        {
            return await GetUserProfileByIdAsync(userId, request.UseCache, cancellationToken);
        }

        try
        {
            var result = await repository.FindOneAsync(x => x.MsalObjectId == msalObjectId, cancellationToken);
            if (result != null)
            {
                msalToUserIds.TryAdd(result.MsalObjectId, result.Id);
                await cacheService.SetAsync(cacheKey, msalToUserIds, TimeSpan.FromDays(1), cancellationToken);
            }

            return mapper.Map<UserProfileDto>(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not locate user profile with MSAL ID: {MsalObjectId}", msalObjectId);
            throw;
        }
    }

    private async Task<UserProfileDto?> GetUserProfileByIdAsync(string userProfileId, bool useCache, CancellationToken cancellationToken)
    {
        if (useCache)
        {
            return await cacheService.GetOrCreateAsync(
                $"{CacheKeys.UserProfilePrefix}-{userProfileId}",
                async () =>
                {
                    var result = await repository.GetByIdAsync(userProfileId, cancellationToken);
                    return mapper.Map<UserProfileDto>(result);
                },
                TimeSpan.FromDays(1),
                cancellationToken);
        }

        var entity = await repository.GetByIdAsync(userProfileId, cancellationToken);
        return mapper.Map<UserProfileDto>(entity);
    }

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
