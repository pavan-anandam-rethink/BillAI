using Rethink.Services.Common.Enums.Billing;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Interfaces
{
    public interface IClaimRepository
    {
        Task UpdateClaimDetailsAsync(int claimId, ClaimStatus status, string error = null);
        Task<string> GetErrorMessageAsync(ClaimErrorNumber errorNumber);
        Task<int?> GetErrorMessageIdAsync(ClaimErrorNumber errorNumber);
        Task<int?> GetLatestClaimSubmissionIdAsync(int claimId);
        Task SaveClaimValidationErrorAsync(int claimId, int claimSubmissionId, int errorMessageId, string contextMessage, ClaimErrorSource errorSource);
    }
}
