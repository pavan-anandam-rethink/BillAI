

using LoginService.Web.Models;

namespace LoginService.Web.Interfaces
{
    public interface IUserProfileService
    {
        Task<UserProfile> GetUserProfileByMsalObjectId(string msalObjectId, bool shouldUseCache);
    }
}
