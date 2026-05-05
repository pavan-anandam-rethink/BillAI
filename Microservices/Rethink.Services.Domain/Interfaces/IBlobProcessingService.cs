using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Interfaces
{
    public interface IBlobProcessingService
    {
        Task<Response<BlobContainerClient>> CreateBlobContainerAsync(string containerName);

        Task<Response<BlobContentInfo>> UploadIntoContainerAsync(string containerName, string fileName,
            MemoryStream fileStream);

        Task<MemoryStream> DownloadBlobFromContainerAsync(string containerName, string fileName);
        Task<Response> DeleteContainerAsync(string containerName);
        Task DeleteBlobFromContainerAsync(string containerName, string fileName);
    }
}