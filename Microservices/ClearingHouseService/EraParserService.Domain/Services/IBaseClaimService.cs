using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models.Claim;
using System.Threading.Tasks;

namespace EraParserService.Domain.Services
{
    public interface IBaseClaimService
    {
        Task UpdateClaimSubmissionStatus(ClaimSubmissionEntity submissionEntity, ClaimSubmissionStatus status);
        Task UpdateClaimStatus(int id, int accountInfoId, ClaimStatus status);
        Task<int> GetAccountInfoById(int claimId);
        Task AddResponseHistory(int claimId, ClaimActionMode mode, ClaimAction action, ClaimHistoryAction claimHistoryAction);
        Task AddClearingHouseDetailsAsync(ClearingHouseDetailsSaveModel clearingHouseDetailsSaveModel, bool commitImmediately = true);
        Task SetHistoryActionDate(ClearingHouseResponseDetailsEntity alreadySaved277Response);
    }
}
