using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Models.Claim.History;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Interfaces
{
    public interface IClaimHistoryService
    {
        Task AddAsync(ClaimHistorySaveModel saveModel, bool commitImmediately = true);

        Task AddAsync(ClaimHistoryFieldSaveModel saveModel, bool commitImmediately = false);

        Task AddAsync(ClaimHistoryVersionSaveModel saveModel, bool commitImmediately = false);
        Task AddAsync(List<ClaimHistorySaveModel> saveModels, bool commitImmediately = true);
        Task AddAsync(List<ClaimHistoryFieldSaveModel> saveModels, bool commitImmediately = true);
        Task<List<ClaimHistoryModel>> GetAllAsync(int claimId, int accountInfoId, int memberId);
        Task<List<ClaimHistoryModel>> GetAllAsync(int claimId);

        Task<List<ClaimHistoryActionEntity>> GetClaimHistoryActionsAsync();

        Task UpdateHistoryFor277(ClearingHouseResponseDetailsEntity alreadySaved277Response);
    }
}
