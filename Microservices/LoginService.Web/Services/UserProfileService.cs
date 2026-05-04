using AutoMapper;
using LoginService.Web.Interfaces;
using LoginService.Web.Models;
using LoginService.Web.Repositories.NoSql;
using Rethink.Services.Common.Cache;
using System.Text;

namespace LoginService.Web.Services
{
    public class UserProfileService : IUserProfileService
    {
        private const string CacheKey = nameof(UserProfileService) + "-cache";

        private readonly IUserProfileRepository _repository;
        private readonly ICacheManager _cacheManager;
        private readonly IMapper _mapper;

        public UserProfileService(
            IUserProfileRepository repository,
            IMapper mapper,
            ICacheManager cacheManager)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
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
                var result = await _repository.FindOneAsync((x) => x.MsalObjectId == msalObjectId);
                if (result != null)
                {
                    msalToUserIds.TryAdd(result.MsalObjectId, result.Id);
                    await _cacheManager.SetAsync(cacheKey, msalToUserIds, CachingDuration.OneDay);
                }

                return _mapper.Map<UserProfile>(result);
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
                var result = await _repository.FindOneAsync((x) => x.Id == userProfileId);
                return _mapper.Map<UserProfile>(result);
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
                // Convert the Base64 string to a byte array
                byte[] base64EncodedBytes = Convert.FromBase64String(base64String);

                // Convert the byte array to a string using the original encoding (UTF-8 is common)
                return Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch (FormatException e)
            {
                // Handle cases where the input string is not a valid Base64 format
                Console.WriteLine($"Error decoding Base64 string: {e.Message}");
                return null; // Or handle the error as appropriate for your application
            }
        }

    }
}
