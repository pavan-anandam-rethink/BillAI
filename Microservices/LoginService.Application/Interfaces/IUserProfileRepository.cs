using LoginService.Domain.Models;

namespace LoginService.Application.Interfaces
{
    public interface IUserProfileRepository
    {
        Task<UserProfile?> FindByMsalObjectIdAsync(string msalObjectId);
        Task<UserProfile?> FindByIdAsync(string userProfileId);
    }
}
