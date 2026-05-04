using BillingService.Domain.Interfaces.Files;
using Rethink.Services.Common.Services;
using System;

namespace BillingService.Domain.Services.Files
{
    public class FileService : BaseService, IFileService
    {
        private readonly IFileManagerService _fileManager;

        public FileService(IFileManagerService fileManager)
        {
            _fileManager = fileManager;
        }

        public string PrepareFolderForEncounterAttachmentFile(int accountId, string fileName, string folderName, bool isHealthCare, int? referenceId = null, string? claimidentifier = null)
        {
            if (folderName.Length == 0 || fileName.Length == 0)
            {
                return null;
            }           

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var uniqueId = Guid.NewGuid();
            string subFolderName = string.IsNullOrEmpty(claimidentifier)
                ? $"{timestamp}_{uniqueId}"
                : $"{claimidentifier}_{timestamp}_{uniqueId}";


            var baseFilePath = _fileManager.BuildBaseFilePathForEncounterAttachment(accountId, folderName, subFolderName, null);
            return baseFilePath;
        }

        public string PrepareFolderForERAErrorFile(int accountId)
        {
            string folderName = $"{DateTime.Now:yyyMMdd}";

            var baseFilePath = _fileManager.BuildBaseFilePathForERAErrors(accountId, folderName);
            return baseFilePath;
        }
    }
}
