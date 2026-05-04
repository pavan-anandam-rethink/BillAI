using Billing.FolderStructure.Core.Models;
using Rethink.Services.Common.Entities.Billing.Claim;

namespace Billing.FolderStructure.Core.Utils
{
    public interface IBillingFilePath
    {
        Task<string> CreateFolderPath(BillingRequest billingRequest);
        Task<(string containerName, string fullFilePath)> SplitFilePath(string filePath);
        Task<TransactionControlNumberModel> GetTransactionControlNumber(string ediData);
        Task<ClaimSubmissionEntity> FetchClaimSubmissionDataForERA(TransactionControlNumberModel model);
        Task<ClaimSubmissionEntity> FetchClaimSubmissionDataForManualERA(TransactionControlNumberModel model, int accountInfoId);
        Task<string> GetEdiFilesFromBlob(ClaimEdiFilesModel model);
        Task AddOrUpdateBlobFilePath(ClaimEdiFilesModel model);
        Task<string> GetEdiFileType(BillingRequest billingRequest);
    }
}
