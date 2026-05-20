using LoginService.Domain.Models;

namespace LoginService.Application.Interfaces
{
    public interface IUserProfileService
    {
        Task<UserProfile> GetUserProfileByMsalObjectId(string msalObjectId, bool shouldUseCache);
    }
}
