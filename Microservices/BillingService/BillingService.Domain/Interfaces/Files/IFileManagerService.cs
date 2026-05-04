using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Files
{
    public interface IFileManagerService
    {
        Task<bool> UploadFileAsync(string path, string fileName, Stream fileStream, string containerName = "");
        Task<bool> UploadFileAsync(string path, string fileName, byte[] fileBytes);
        Task<Stream> GetFile(string fullPath);
        Task<byte[]> GetFileByte(string fullPath);
        Task<bool> DeleteFile(string fullPath);
        Task<bool> CopyFile(string fullFilePath, string destinationfilePath);
        Task<bool> FileExists(string fullPath);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullPath">File Path</param>
        /// <param name="expiryTime">Expiry Time in minutes</param>
        /// <returns></returns>
        Task<string> GetFileUrl(string fullPath, int expiryTime = 1, string containerName = "");

        List<string> InvalidFileExtensions { get; }
        List<string> AcceptableImageFileExtensions { get; }
        List<string> AcceptableVideoFileExtensions { get; }


        string BuildBaseFilePath(int accountId, int? childProfileId, int? memberId, int folderId);
        string BuildBaseFilePathForEncounterAttachment(int accountId, string folderName, string subFolderName, int? encounterId = null);
        string BuildBaseFilePathForERAErrors(int accountId, string folderName);
    }
}
