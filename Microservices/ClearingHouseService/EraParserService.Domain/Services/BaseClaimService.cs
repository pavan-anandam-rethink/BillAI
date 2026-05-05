using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EraParserService.Domain.Services
{
    public class BaseClaimService : BaseService, IBaseClaimService
    {
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IRepository<BillingDbContext, ClaimSubmissionEntity> _claimSubmissionRepository;
        private readonly IRepository<BillingDbContext, ClearingHouseResponseDetailsEntity> _clearingHouseDetailsRespository;
        private readonly IClaimHistoryService _claimHistoryService;
        private readonly IMessageBus _bus;
        public BaseClaimService(
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IRepository<BillingDbContext, ClaimSubmissionEntity> claimSubmissionRepository,
            IRepository<BillingDbContext, ClearingHouseResponseDetailsEntity> clearingHouseDetailsRespository,
            IClaimHistoryService claimHistoryService,
            IMessageBus bus)
        {
            _claimRepository = claimRepository;
            _claimSubmissionRepository = claimSubmissionRepository;
            _clearingHouseDetailsRespository = clearingHouseDetailsRespository;
            _claimHistoryService = claimHistoryService;
            _bus = bus;
        }

        public async Task UpdateClaimStatus(int id, int accountInfoId, ClaimStatus status)
        {
            try
            {
                var claimEntity =await _claimRepository.Query().FirstOrDefaultAsync(x => x.AccountInfoId == accountInfoId && x.DateDeleted == null && x.Id == id);

                claimEntity.ClaimStatus = status;
                MarkUpdated(claimEntity, 0);

                _claimRepository.Update(claimEntity);
                await _claimRepository.CommitAsync();

                //For Updating the statuses in AR report
                await _bus.SendAsync(PrepareClaimTransaction(claimEntity.Id, ClaimTransactionType.submitClaim), Topics.RT_Billing_ProcessClaimTxn);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating claim status: " + $"claim={id}, memberId={0}, submissionStatus={status}\n" + $"Error: {ex.Message}", ex);
            }
        }

        public async Task UpdateClaimSubmissionStatus(ClaimSubmissionEntity submissionEntity, ClaimSubmissionStatus status)
        {
            try
            {
                submissionEntity.SubmissionStatus = status;

                MarkUpdated(submissionEntity, 0);
                _claimSubmissionRepository.Update(submissionEntity);
                await _claimSubmissionRepository.CommitAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating claim submission status: " + $"submission id={submissionEntity.Id}, memberId={0}, submissionStatus={status}\n" + $"Error: {ex.Message}", ex);
            }
        }

        public async Task<int> GetAccountInfoById(int claimId)
        {
            try
            {
                var claimEntity = await _claimRepository.Query().FirstOrDefaultAsync(x => x.Id == claimId && x.DateDeleted == null);
                if (claimEntity != null)
                {
                    return claimEntity.AccountInfoId;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving claim status for claim id={claimId}: {ex.Message}", ex);
            }
        }

        public async Task AddResponseHistory(int claimId, ClaimActionMode mode, ClaimAction action, ClaimHistoryAction claimHistoryAction)
        {
            await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
            {
                ClaimId = claimId,
                MemberId = 0,
                Mode = mode,
                ClaimAction = action,
                ClaimHistoryAction = claimHistoryAction,
                NewValue = ""
            }, true);
        }



        public async Task AddClearingHouseDetailsAsync(ClearingHouseDetailsSaveModel clearingHouseDetailsSaveModel, bool commitImmediately = true)
        {
            var clearingHouseResponse = new ClearingHouseResponseDetailsEntity
            {
                ClaimId = clearingHouseDetailsSaveModel.claimId,
                ClaimValidationErrorId = clearingHouseDetailsSaveModel.validationErrorId,
                BatchId = clearingHouseDetailsSaveModel.batchId,
                ResponseFileTypeId = clearingHouseDetailsSaveModel.fileTypeId,
                IsAccepted = clearingHouseDetailsSaveModel.isAccepted,
                FileIdentifier = clearingHouseDetailsSaveModel.fileIdentifier,
                DownloadDateTime = clearingHouseDetailsSaveModel.downloadDateTime,
                ClearingHouseId = clearingHouseDetailsSaveModel.clearingHouseId
            };
            MarkCreated(clearingHouseResponse, 0);

            await _clearingHouseDetailsRespository.AddAsync(clearingHouseResponse);
            if (commitImmediately) await _clearingHouseDetailsRespository.CommitAsync();
        }

        public async Task SetHistoryActionDate(ClearingHouseResponseDetailsEntity alreadySaved277Response)
        {
            await _claimHistoryService.UpdateHistoryFor277(alreadySaved277Response);
        }
    }
}
