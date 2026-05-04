using BillingService.Domain.Models.ClearingHouse;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    /// <summary>
    /// Service interface for validating clearinghouse SFTP credentials
    /// </summary>
    public interface IClearinghouseCredentialValidationService
    {
        /// <summary>
        /// Validates SFTP credentials for all active clearinghouses
        /// </summary>
        /// <returns>True if all clearinghouse connections are valid, false otherwise</returns>
        Task<ClearinghouseApiValidationResponse> ValidateAllClearinghousesAsync();
    }

}
