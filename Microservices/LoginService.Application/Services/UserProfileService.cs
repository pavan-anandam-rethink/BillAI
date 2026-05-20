using LoginService.Application.Interfaces;
using LoginService.Domain.Models;
using Rethink.Services.Common.Cache;
using System.Text;

namespace LoginService.Application.Services
{
    public class UserProfileService : IUserProfileService
    {
        private const string CacheKey = nameof(UserProfileService) + "-cache";

        private readonly IUserProfileRepository _repository;
        private readonly ICacheManager _cacheManager;

        public UserProfileService(
            IUserProfileRepository repository,
            ICacheManager cacheManager)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        }

        public virtual async Task<UserProfile> GetUserProfileByMsalObjectId(string msalObjectId, bool shouldUseCache)
        {
            msalObjectId = DecodeBase64(msalObjectId);
            var cacheKey = $"{CacheKey}-msalMaps";
            var msalToUserIds = await _cacheManager.GetAsync<Dictionary<string, string>>(cacheKey, () => Task.FromResult(new Dictionary<string, string>()), CachingDuration.OneDay);
            if (msalToUserIds.TryGetValue(msalObjectId, out var userId))
            {
                return await this.GetUserProfileById(userId, shouldUseCache);
            }
            try
            {
                var result = await _repository.FindByMsalObjectIdAsync(msalObjectId);
                if (result != null)
                {
                    msalToUserIds.TryAdd(result.MsalObjectId, result.Id);
                    await _cacheManager.SetAsync(cacheKey, msalToUserIds, CachingDuration.OneDay);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not locate user profile with MSAL ID: {msalObjectId}", ex);
            }
        }

        public async Task<UserProfile> GetUserProfileById(string userProfileId, bool shouldUseCache)
        {
            var action = new Func<Task<UserProfile>>(async () =>
            {
                return await _repository.FindByIdAsync(userProfileId);
            });

            if (shouldUseCache)
            {
                return await _cacheManager.GetAsync(
                    $"{CacheKey}-{userProfileId}",
                    action,
                    CachingDuration.OneDay);
            }

            return await action();
        }

        private static string DecodeBase64(string base64String)
        {
            if (string.IsNullOrEmpty(base64String))
            {
                return string.Empty;
            }

            try
            {
                byte[] base64EncodedBytes = Convert.FromBase64String(base64String);
                return Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch (FormatException e)
            {
                Console.WriteLine($"Error decoding Base64 string: {e.Message}");
                return null;
            }
        }
    }
}
