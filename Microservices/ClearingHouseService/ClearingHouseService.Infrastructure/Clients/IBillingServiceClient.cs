namespace ClearingHouseService.Infrastructure.Clients
{
    /// <summary>
    /// Abstraction for BillingService API communication.
    /// Extracted from the CommonHelper to provide clean separation of concerns.
    /// </summary>
    public interface IBillingServiceClient
    {
        /// <summary>
        /// Calls the BillingService to generate EDI data for a claim.
        /// </summary>
        Task<(bool Success, string Result)> GenerateEdiDataAsync(object claimModel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calls the BillingService to generate 270 EDI eligibility data.
        /// </summary>
        Task<(bool Success, string Result)> Generate270EdiDataAsync(object eligibilityRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a file to blob storage via the BillingService.
        /// </summary>
        Task<(bool Success, string Result)> UploadFileToBlobStorageAsync(object fileModel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads SFTP response files to blob storage via the BillingService.
        /// </summary>
        Task<(bool Success, string Result)> UploadSftpFilesToBlobStorageAsync(object fileData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reapplies PR adjustment after secondary billing.
        /// </summary>
        Task<bool> ReapplyPrAdjustmentAfterSecondaryBillingAsync(int claimId, CancellationToken cancellationToken = default);
    }
}
