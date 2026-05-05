using ClearingHouseService.Web.Service;

namespace ClearingHouseService.Web.Interface
{
    // This interface defines a contract for resolving credentials based on clearing house details.
    // Implementations of this interface will provide the logic to retrieve the necessary credentials (such as URL, port, username, password, and remote directory)
    // required for uploading files to different clearing houses.
    public interface ICredentialResolver
    {
        Task<ClearingHouseDetailsModel> ResolveAsync(ClearingHouseDetailsModel clearingHouse);
    }
}
