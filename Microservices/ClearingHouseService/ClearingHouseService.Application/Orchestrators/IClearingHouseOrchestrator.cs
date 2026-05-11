using ClearingHouseService.Application.DTOs;
using ClearingHouseService.Domain.Entities;

namespace ClearingHouseService.Application.Orchestrators
{
    /// <summary>
    /// Orchestrates the multi-step clearing house claim submission workflow:
    /// EDI generation → validation → upload → status tracking → response processing.
    /// </summary>
    public interface IClearingHouseOrchestrator
    {
        /// <summary>
        /// Orchestrates the full claim submission workflow.
        /// </summary>
        /// <param name="claimId">The claim identifier.</param>
        /// <param name="ediData">The generated EDI data.</param>
        /// <param name="clearingHouseId">The clearing house to submit to.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of the submission operation.</returns>
        Task<ClaimSubmissionResult> SubmitClaimAsync(int claimId, string ediData, int clearingHouseId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Orchestrates downloading and processing response files from a clearing house.
        /// </summary>
        /// <param name="clearingHouseId">The clearing house to download from.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of downloaded files with their data.</returns>
        Task<List<(MemoryStream Data, string FileName)>> DownloadResponsesAsync(int clearingHouseId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the connection to all configured clearing houses.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation results for each clearing house.</returns>
        Task<List<TransmissionResult>> ValidateAllConnectionsAsync(CancellationToken cancellationToken = default);
    }
}
