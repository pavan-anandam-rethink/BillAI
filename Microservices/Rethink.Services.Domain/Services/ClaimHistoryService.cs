using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Services
{
    public class ClaimHistoryService : BaseService, IClaimHistoryService
    {
        private readonly IRepository<BillingDbContext, ClaimHistoryEntity> _claimHistoryRepository;
        private readonly IRepository<BillingDbContext, ClaimHistoryActionEntity> _claimHistoryActionRepository;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;

        public ClaimHistoryService(
            IRepository<BillingDbContext, ClaimHistoryEntity> claimHistoryRepository,
            IRepository<BillingDbContext, ClaimHistoryActionEntity> claimHistoryActionRepository,
            IRethinkMasterDataMicroServices rethinkServices)
        {
            _claimHistoryRepository = claimHistoryRepository;
            _claimHistoryActionRepository = claimHistoryActionRepository;
            _rethinkServices = rethinkServices;
        }

        public async Task AddAsync(ClaimHistorySaveModel saveModel, bool commitImmediately = true)
        {
            var historyEntry = new ClaimHistoryEntity()
            {
                ClaimId = saveModel.ClaimId,
                Mode = saveModel.Mode,
                ClaimAction = saveModel.ClaimAction,
                ClaimHistoryAction = saveModel.ClaimHistoryAction,
                ActionDate = saveModel.ActionDate ?? EstDateTime,
                OldValue = saveModel.OldValue,
                NewValue = saveModel.NewValue,
                RethinkUser = saveModel.ImpersonationUserName ?? null
            };

            MarkCreated(historyEntry, saveModel.MemberId);

            await _claimHistoryRepository.AddAsync(historyEntry);
            if (commitImmediately) await _claimHistoryRepository.CommitAsync();
        }

        public async Task AddAsync(ClaimHistoryFieldSaveModel saveModel, bool commitImmediately = false)
        {
            var historyEntry = new ClaimHistoryEntity()
            {
                ClaimId = saveModel.ClaimId,
                Mode = saveModel.Mode,
                ClaimAction = saveModel.ClaimAction,
                ClaimHistoryAction = saveModel.ClaimHistoryAction,
                ClaimHistoryField = saveModel.ClaimHistoryField,
                ActionDate = saveModel.ActionDate ?? EstDateTime,
                OldValue = saveModel.OldValue,
                NewValue = saveModel.NewValue,
                RethinkUser = saveModel.ImpersonationUserName ?? null
            };

            MarkCreated(historyEntry, saveModel.MemberId);

            await _claimHistoryRepository.AddAsync(historyEntry);
            if (commitImmediately) await _claimHistoryRepository.CommitAsync();
        }

        public async Task AddAsync(List<ClaimHistorySaveModel> saveModels, bool commitImmediately = true)
        {
            var entities = saveModels.Select(x =>
            {
                var historyEntry = new ClaimHistoryEntity()
                {
                    ClaimId = x.ClaimId,
                    Mode = x.Mode,
                    ClaimAction = x.ClaimAction,
                    ClaimHistoryAction = x.ClaimHistoryAction,
                    ActionDate = x.ActionDate ?? EstDateTime,
                    OldValue = x.OldValue,
                    NewValue = x.NewValue,
                    RethinkUser = x.ImpersonationUserName ?? null
                };

                MarkCreated(historyEntry, x.MemberId);
                return historyEntry;
            }).ToList();
            await _claimHistoryRepository.AddRangeAsync(entities);
            if (commitImmediately) await _claimHistoryRepository.CommitAsync();
        }

        public async Task AddAsync(List<ClaimHistoryFieldSaveModel> saveModels, bool commitImmediately = true)
        {
            var entities = saveModels.Select(x =>
            {
                var historyEntry = new ClaimHistoryEntity()
                {
                    ClaimId = x.ClaimId,
                    Mode = x.Mode,
                    ClaimAction = x.ClaimAction,
                    ClaimHistoryAction = x.ClaimHistoryAction,
                    ClaimHistoryField = x.ClaimHistoryField,
                    ActionDate = x.ActionDate ?? EstDateTime,
                    OldValue = x.OldValue,
                    NewValue = x.NewValue
                };

                MarkCreated(historyEntry, x.MemberId);
                return historyEntry;
            }).ToList();
            await _claimHistoryRepository.AddRangeAsync(entities);
            if (commitImmediately) await _claimHistoryRepository.CommitAsync();
        }

        public async Task AddAsync(ClaimHistoryVersionSaveModel saveModel, bool commitImmediately = false)
        {
            var historyEntry = new ClaimHistoryEntity()
            {
                ClaimId = saveModel.ClaimId,
                Mode = saveModel.Mode,
                ClaimAction = saveModel.ClaimAction,
                ClaimHistoryAction = saveModel.ClaimHistoryAction,
                ClaimVersionId = saveModel.ClaimVersionId,
                ActionDate = saveModel.ActionDate ?? EstDateTime,
                OldValue = saveModel.OldValue,
                NewValue = saveModel.NewValue,
                RethinkUser = saveModel.ImpersonationUserName ?? null
            };

            MarkCreated(historyEntry, saveModel.MemberId);

            await _claimHistoryRepository.AddAsync(historyEntry);
            if (commitImmediately) await _claimHistoryRepository.CommitAsync();
        }

        public async Task<List<ClaimHistoryModel>> GetAllAsync(int claimId, int accountInfoId, int memberId)
        {
            var historyRecords = await _claimHistoryRepository.Query()
                .Where(x => x.DateDeleted == null && x.ClaimId == claimId)
                .ToListAsync();

            var memberIdList = historyRecords.GroupBy(x => x.CreatedBy).Select(x => x.Key).ToList();

            var memberIds = string.Empty;

            memberIds = "memberIds=" + Convert.ToString(memberId);

            var members = await _rethinkServices.GetMembersAsync(accountInfoId, memberIds);

            var result = historyRecords
                .Select(x => new ClaimHistoryModel
                {
                    ActionId = x.ClaimAction,
                    HistoryActionId = x.ClaimHistoryAction,
                    ChangeBy = x.Mode == ClaimActionMode.System ? "System" : GetMemberFullName(members.data, memberId),//!x.ModifiedBy.HasValue || x.ModifiedBy == 0 ? "System" : GetMemberFullName(members.data, x.ModifiedBy.Value),
                    ChangeDate = x.ActionDate,
                    Mode = x.Mode,
                    FieldId = (int)(x.ClaimHistoryField ?? default(int)),
                    OldValue = decimal.TryParse(x.OldValue, out var oldVal) ? oldVal.ToString("0.##") : x.OldValue,
                    NewValue = decimal.TryParse(x.NewValue, out var newVal) ? newVal.ToString("0.##") : x.NewValue,
                    ClaimVersionHistoryId = x.ClaimVersionId,
                    RethinkUser = string.IsNullOrWhiteSpace(x.RethinkUser)
                                    ? "N/A"
                                    : x.RethinkUser,

                })
                .ToList();

            return result;
        }

        public async Task<List<ClaimHistoryModel>> GetAllAsync(int claimId)
        {
            var historyRecords = await _claimHistoryRepository.Query()
                .Where(x => x.DateDeleted == null && x.ClaimId == claimId)
                .ToListAsync();

            var result = historyRecords
               .Select(x => new ClaimHistoryModel
               {
                   ActionId = x.ClaimAction,
                   HistoryActionId = x.ClaimHistoryAction,
                   ChangeDate = x.ActionDate,
                   Mode = x.Mode,
                   FieldId = x.Id,
                   OldValue = x.OldValue,
                   NewValue = x.NewValue,
                   ClaimVersionHistoryId = x.ClaimVersionId,
                   RethinkUser = string.IsNullOrWhiteSpace(x.RethinkUser)
                                        ? "N/A"
                                        : x.RethinkUser,

               })
               .ToList();

            return result;

        }

        public async Task UpdateHistoryFor277(ClearingHouseResponseDetailsEntity alreadySaved277Response)
        {
            var history277 = await _claimHistoryRepository.Query().Where(x => x.ClaimId == alreadySaved277Response.ClaimId &&
                                                                                x.DateDeleted == null).ToListAsync();
            if (alreadySaved277Response.IsAccepted)
            {
                history277 = history277.Where(x => x.ClaimHistoryAction == ClaimHistoryAction.ClaimResponseAccepted277).ToList();
            }
            else if (!alreadySaved277Response.IsAccepted && alreadySaved277Response.ClaimValidationErrorId > 0)
            {
                history277 = history277.Where(x => x.ClaimHistoryAction == ClaimHistoryAction.ClaimResponseRejected277).ToList();
            }
            else if (!alreadySaved277Response.IsAccepted && (alreadySaved277Response.ClaimValidationErrorId == 0 || alreadySaved277Response.ClaimValidationErrorId == null))
            {
                history277 = history277.Where(x => x.ClaimHistoryAction == ClaimHistoryAction.ClaimResponseReceived277).ToList();
            }
            var historyEntity = history277.FirstOrDefault();
            historyEntity.ActionDate = EstDateTime;
            _claimHistoryRepository.Update(historyEntity);
            await _claimHistoryRepository.SaveChangesAsync();
        }

        public async Task<List<ClaimHistoryActionEntity>> GetClaimHistoryActionsAsync()
        {
            return await (await _claimHistoryActionRepository.GetAllAsync()).ToListAsync();
        }



        private string GetMemberFullName(List<RethinkAccountMember> members, int memberId)
        {
            var member = members.FirstOrDefault(x => x.id == memberId);
            if (member != null)
            {
                return string.Format("{0} {1} {2}", member.firstName, member.middleName ?? string.Empty, member.lastName);
            }

            return string.Empty;
        }
    }
}
