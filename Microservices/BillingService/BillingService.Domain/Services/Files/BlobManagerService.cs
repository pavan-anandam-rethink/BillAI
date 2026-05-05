using BillingService.Domain.Interfaces.Files;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Files
{
    public class BlobManagerService : IFileManagerService
    {
        private string ContainerName { get { return "rtafiles"; } }
        private string EraContainerName { get { return "eraerrorfiles"; } }
        private string StorageConnectionString { get; set; }

        public BlobManagerService(IConfiguration configuration, IKeyVaultProviderService keyVaultProviderService)
        {
            this.StorageConnectionString = keyVaultProviderService.GetSecretAsync(configuration["ConnectionStrings:BlobStorage:ConnectionString"]).Result;
        }

        public List<string> InvalidFileExtensions => new List<string> { ".exe" };
        public List<string> AcceptableImageFileExtensions => new List<string> { ".gif", ".png", ".jpeg", ".jpg" };
        public List<string> AcceptableVideoFileExtensions => new List<string> { ".mp4", ".mov", ".avi" };
        public List<string> AcceptableDocFileExtensions => new List<string> { ".doc", ".docx" };
        public List<string> AcceptablePrintableFileExtensions => new List<string> { ".pdf" };

        public async Task<bool> UploadFileAsync(string path, string fileName, Stream fileStream, string containerName = "")
        {
            if (string.IsNullOrEmpty(containerName)) containerName = ContainerName;
            if (path.Contains("\\"))
            {
                throw new Exception("Invalid Blob Path");
            }

            var container = await GetContainer(containerName);

            path = normalizePath(path);
            var dir = container.GetDirectoryReference(path);

            var blockBlob = dir.GetBlockBlobReference(fileName);


            await blockBlob.UploadFromStreamAsync(fileStream);
            return true;
        }

        private string normalizePath(string path)
        {
            if (path.StartsWith("/"))
                path = path.Substring(1, path.Length - 1);

            if (path.StartsWith("\\") || path.StartsWith("~/"))
            {
                path = path.Substring(2);
            }

            return path;
        }

        // SAS at container level
        private async Task<string> GetSASUrl()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference(ContainerName);
            await container.CreateIfNotExistsAsync();

            string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

            BlobContainerPermissions containerPermissions = new BlobContainerPermissions();
            containerPermissions.SharedAccessPolicies.Add(
              token, new SharedAccessBlobPolicy()
              {
                  SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-1),
                  SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(50),
                  Permissions = SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Read
              });
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Off;
            await container.SetPermissionsAsync(containerPermissions);

            string sas = container.GetSharedAccessSignature(new SharedAccessBlobPolicy(), token);

            return sas;
        }

        // SAS at file level
        private async Task<string> GetBlobSasUrl(string fullPath, int expiryTime, string containerName)
        {
            CloudBlockBlob blob = await GetBlob(fullPath, containerName);

            string sasToken = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-2),
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(expiryTime),
            });

            return blob.Uri.AbsoluteUri + sasToken;
        }

        public async Task<string> GetFileUrl(string fullPath, int expiryTime = 1, string containerName = "")
        {
            if (string.IsNullOrEmpty(containerName)) containerName = ContainerName;
            if (String.IsNullOrEmpty(fullPath))
                return null;
            if (fullPath.StartsWith("~"))
            {
                fullPath = fullPath.Substring(1);
            }

            if (!fullPath.StartsWith("/"))
                fullPath = "/" + fullPath;

            return await GetBlobSasUrl(fullPath, expiryTime, containerName);
            //return blobPath + fullPath + SasUrl;
        }

        public async Task<bool> UploadFileAsync(string path, string fileName, byte[] fileBytes)
        {
            using (var fileStream = new MemoryStream(fileBytes))
            {
                await UploadFileAsync(path, fileName, fileStream);
                return true;
            }
        }

        public async Task<bool> CopyFile(string fullPath, string destinationfilePath)
        {
            var container = await GetContainer(ContainerName);
            fullPath = normalizePath(fullPath);
            destinationfilePath = normalizePath(destinationfilePath);

            var src = container.GetBlockBlobReference(fullPath);
            var destination = container.GetBlockBlobReference(destinationfilePath);
            await destination.StartCopyAsync(src);
            return true;

        }

        public async Task<byte[]> GetFileByte(string fullPath)
        {
            return (await GetFileMemoryStream(fullPath)).ToArray();
        }

        public async Task<Stream> GetFile(string fullPath)
        {
            return await GetFileMemoryStream(fullPath);

        }

        private async Task<MemoryStream> GetFileMemoryStream(string fullPath)
        {
            var blob = await GetBlob(fullPath, "");

            var memoryStream = new MemoryStream();

            await blob.DownloadToStreamAsync(memoryStream);
            memoryStream.Seek(0, 0);
            return memoryStream;

        }

        private async Task<CloudBlockBlob> GetBlob(string fullPath, string containerName)
        {
            var container = await GetContainer(containerName);

            fullPath = normalizePath(fullPath);
            return container.GetBlockBlobReference(fullPath);

        }

        private async Task<CloudBlobContainer> GetContainer(string containerName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            return container;
        }


        public async Task<bool> DeleteFile(string fullPath)
        {
            var container = await GetContainer(ContainerName);
            if (fullPath.StartsWith("/"))
                fullPath = fullPath.Substring(1, fullPath.Length - 1);
            var blob = container.GetBlockBlobReference(fullPath);
            if (await blob.ExistsAsync())
            {
                await blob.DeleteAsync();
                return true;
            }

            return false;

        }

        public async Task<bool> FileExists(string fullPath)
        {
            var blob = await GetBlob(fullPath, "");
            return await blob.ExistsAsync();
        }

        public string BuildBaseFilePath(int accountId, int? childProfileId, int? memberId, int folderId)
        {
            if (childProfileId.HasValue)
            {
                return String.Format(
                    "{0}/{1}/{2}/{3}/",
                    "UploadedFiles",
                    accountId,
                    childProfileId,
                    folderId
                );
            }
            else if (memberId.HasValue)
            {
                return String.Format(
                    "{0}/{1}/StaffCabinet/{2}/{3}/",
                    "UploadedFiles",
                    accountId,
                    memberId.Value,
                    folderId
                    );
            }

            return String.Format(
                "{0}/{1}/company/{2}/",
                "UploadedFiles",
                accountId,
                folderId
            );
        }

        public string BuildBaseFilePathForEncounterAttachment(int accountId, string folderName, string subFolderName, int? encounterId = null)
        {
            return String.Format(
                $"UploadedFiles/{accountId}/{folderName}/{subFolderName}/");
        }

        public string BuildBaseFilePathForERAErrors(int accountId, string folderName)
        {
            return String.Format(
                $"{accountId}/{folderName}/");
        }
    }
}
