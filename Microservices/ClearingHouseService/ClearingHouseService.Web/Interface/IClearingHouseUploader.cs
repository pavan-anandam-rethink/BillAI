using ClearingHouseService.Web.Service;
using ClearingHouseService.Web.Models;

namespace ClearingHouseService.Web.Interface
{
    // This interface defines a contract for uploading files to a clearing house.
    // Implementations of this interface will provide the logic to upload files (such as EDI data) to different clearing houses using various protocols (e.g., SFTP).
    public interface IClearingHouseUploader
    {
        Task<OperationResult> UploadFileToSftpAsync(ClearingHouseDetailsModel clearingHouse,string fileName,Stream fileStream,int claimId);
        Task<List<(MemoryStream, string)>> DownloadFilesFromSftpAsync(ClearingHouseDetailsModel clearingHouse);
        Task<bool> DeleteFileFromSftpAsync(ClearingHouseDetailsModel clearingHouse, string fileName);
        Task<ClearinghouseCredentialValidationResult> ValidateSftpCredentialsAsync(ClearingHouseDetailsModel clearingHouse);
        Task<List<ClearinghouseCredentialValidationResult>> ValidateMultipleClearinghousesAsync(List<ClearingHouseDetailsModel> clearinghouses);
    }
}
