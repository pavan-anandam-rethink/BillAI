using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Rethink.Services.Common.Factories;
using Rethink.Services.Domain.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Services
{
    public class BlobProcessingService : IBlobProcessingService
    {
        private readonly IBlobConnectionFactory _connectionFactory;

        public BlobProcessingService(IBlobConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Response<BlobContainerClient>> CreateBlobContainerAsync(string containerName)
        {
            var client = _connectionFactory.CreateBlobClient();
            var result = await client.CreateBlobContainerAsync(containerName);

            return result;
        }

        public async Task<Response<BlobContentInfo>> UploadIntoContainerAsync(string containerName, string fileName,
            MemoryStream stream)
        {
            var client = _connectionFactory.CreateBlobClient();

            var containerClient = client.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(fileName);
            var result = await blobClient.UploadAsync(stream, true);

            return result;
        }

        public async Task<MemoryStream> DownloadBlobFromContainerAsync(string containerName, string fileName)
        {
            var client = _connectionFactory.CreateBlobClient();
            var containerClient = client.GetBlobContainerClient(containerName.ToLowerInvariant());
            var fileClient = containerClient.GetBlobClient(fileName);
            var fileResponse = await fileClient.DownloadAsync();

            var downloadFileStream = new MemoryStream();
            await fileResponse.Value.Content.CopyToAsync(downloadFileStream);

            return downloadFileStream;
        }

        public async Task DeleteBlobFromContainerAsync(string containerName, string fileName)
        {
            var client = _connectionFactory.CreateBlobClient();
            var containerClient = client.GetBlobContainerClient(containerName);
            var fileClient = containerClient.GetBlobClient(fileName);
            await fileClient.DeleteIfExistsAsync();
        }

        public async Task<Response> DeleteContainerAsync(string containerName)
        {
            var client = _connectionFactory.CreateBlobClient();
            var result = await client.DeleteBlobContainerAsync(containerName);

            return result;
        }
    }
}