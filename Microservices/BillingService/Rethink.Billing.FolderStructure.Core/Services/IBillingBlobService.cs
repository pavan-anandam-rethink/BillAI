using Azure;
using Azure.Storage.Blobs.Models;
using Billing.FolderStructure.Core.Models;

namespace Billing.FolderStructure.Core.Services
{
    public interface IBillingBlobService
    {
        Task CreateBlobContainerAsync(CancellationToken cancellationToken = default);

        Task<string> UploadIntoContainerAsync(string containerName, string blobPath,
            MemoryStream fileStream);

        Task<MemoryStream> DownloadBlobFromContainerAsync(string containerName, string filePath);
        Task DeleteContainerAsync(string containerName);
        Task DeleteBlobFromContainerAsync(string containerName, string filePath);
        Task Update999ReportAsync(string containerName, EDI999Summary summary, string filePath);
        Task Update277DailySummaryReportAsync(string containerName, string reportFileName, string ediContent, List<string> existingClaimIds);
        Task<List<string>> Update277DetailedReportAsync(string containerName, string ediData, string reportFileName);
        Task<string> UploadAvailityFilesToBlobBackupAsync(string containerName, string blobPath, MemoryStream fileStream);

    }
}
