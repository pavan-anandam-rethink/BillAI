using ClearingHouseService.Application.DTOs;

namespace ClearingHouseService.Application.Orchestrators
{
    /// <summary>
    /// Orchestrates the eligibility (270/271) workflow:
    /// EDI 270 generation → submission to clearing house → 271 response parsing → storage.
    /// </summary>
    public interface IEligibilityOrchestrator
    {
        /// <summary>
        /// Orchestrates the full eligibility check workflow.
        /// </summary>
        /// <param name="edi270Data">The generated 270 EDI data.</param>
        /// <param name="clearingHouseId">The clearing house to submit to.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of the eligibility check.</returns>
        Task<EligibilityResult> CheckEligibilityAsync(string edi270Data, int clearingHouseId, CancellationToken cancellationToken = default);
    }
}
