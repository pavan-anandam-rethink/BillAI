using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.BH;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IClaimValidationService
    {
        Task<ClaimEntity> GetClaimInformation(int claimId);
        Task<ClaimSubmissionEntity> GetClaimSubmissionInformation(int claimSubmissionId);
        Task ValidateClaimData(int claimId, int memberId, ClaimEntity claim, ResponsibilitySequenceType responsibilitySequence = ResponsibilitySequenceType.Primary, bool isSaveSubmission = false, int? secondaryFunderId = null);
        Task PrepareClaimSubmission(ClaimEntity claim,
            ClaimSubmissionEntity claimSubmission,
            ClaimSubmissionEntity priorClaimSubmission, int submittingMemberId, int? secondaryFunderId = null);
    }
}
