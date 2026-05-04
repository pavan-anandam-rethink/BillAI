using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using Microsoft.EntityFrameworkCore;
using Quartz.Util;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class ClaimNoteService : BaseService, IClaimNoteService
    {
        private readonly IRepository<BillingDbContext, ClaimNoteEntity> _claimNoteRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimEntity> _paymentClaimRepository;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly IClaimHistoryService _claimHistoryService;

        public ClaimNoteService(
            IRepository<BillingDbContext, ClaimNoteEntity> claimNoteRepository,
            IRepository<BillingDbContext, PaymentClaimEntity> paymentClaimRepository,
            IRethinkMasterDataMicroServices rethinkServices,
            IClaimHistoryService claimHistoryService
            )
        {
            _paymentClaimRepository = paymentClaimRepository;
            _claimNoteRepository = claimNoteRepository;
            _rethinkServices = rethinkServices;
            _claimHistoryService = claimHistoryService;
        }

        public async Task<ActionResponse> GetAllAsync(ClaimNoteGetAllModel model)
        {
            var notes = await _claimNoteRepository.Query()
                .Where(x => x.ClaimId == model.Id)
                .Select(x => new ClaimNote
                {
                    Id = x.Id,
                    ClaimId = x.ClaimId,
                    RemindDate = x.RemindDate,
                    RecievedReminder = x.RecievedReminder,
                    Note = x.Note,
                    CreatedBy = x.CreatedBy,
                    DateCreated = x.DateCreated,
                    ModifiedBy = x.ModifiedBy,
                    DateLastModified = x.DateLastModified,
                    DateDeleted = x.DateDeleted
                })
                .OrderByDescending(x => x.RemindDate).ThenBy(x => x.Note)
                .ToListAsync();

            if (!notes.Any())
            {
                return ActionResponse.SuccessResult(new List<ClaimNote>());
            }

            //new request because of different dbs
            var memberNotesIdList = new List<int?>();
            notes.ForEach(note =>
            {
                memberNotesIdList.Add(note.CreatedBy);
                memberNotesIdList.Add(note.ModifiedBy);
            });

            var memberIds = string.Empty;
            foreach (int? memberId in memberNotesIdList.Distinct())
            {
                memberIds += "memberIds=" + memberId?.ToString() + "&";
            }
            memberIds += memberIds.TrimEnd('&');

            var members = await _rethinkServices.GetMembersAsync(model.AccountInfoId, memberIds);

            notes.ForEach(note =>
            {
                var creator = members.data.Find(member => member.id == note.CreatedBy);
                var modifier = members.data.Find(member => member.id == note.ModifiedBy);

                note.CreatedByName = $"{creator?.firstName} {creator?.lastName}";
                note.ModifiedByName = $"{modifier?.firstName} {modifier?.lastName}";
            });

            return ActionResponse.SuccessResult(notes);
        }

        public async Task<ActionResponse> AddAsync(ClaimNoteSaveModel model)
        {
            if (!model.Note.IsNullOrWhiteSpace())
            {
                var newNote = new ClaimNoteEntity
                {
                    RemindDate = model.RemindDate,
                    Note = model.Note,
                    ClaimId = model.ClaimId
                };

                MarkCreated(newNote, model.MemberId);
                _claimNoteRepository.Add(newNote);

                await _claimNoteRepository.CommitAsync();

                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = newNote.ClaimId,
                    MemberId = model.MemberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.Added,
                    ClaimHistoryAction = ClaimHistoryAction.ClaimNoteAdded,
                    NewValue = newNote.Note,
                });
            }
            return ActionResponse.SuccessResult();
        }

        public async Task<ActionResponse> AddToClaimsAsync(ClaimNoteRequestModel model)
        {
            var claimNotes = new List<ClaimNoteEntity>();

            foreach (var claimNoteModel in model.ClaimNoteModels)
            {
                //var claimId = claimNoteModel.isClaimId ? claimNoteModel.ClaimId : await _paymentClaimRepository.Query().Where(x => x.Id == claimNoteModel.ClaimId).Select(x => x.ClaimId).FirstOrDefaultAsync();
                var newNote = new ClaimNoteEntity
                {
                    RemindDate = claimNoteModel.RemindDate,
                    Note = claimNoteModel.Note,
                    ClaimId = claimNoteModel.ClaimId

                };

                MarkCreated(newNote, model.MemberId);
                claimNotes.Add(newNote);

                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = claimNoteModel.ClaimId,
                    MemberId = model.MemberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.Added,
                    ClaimHistoryAction = ClaimHistoryAction.ClaimNoteAdded,
                    NewValue = newNote.Note,
                });
            }

            await _claimNoteRepository.AddRangeAsync(claimNotes);
            await _claimNoteRepository.CommitAsync();

            return ActionResponse.SuccessResult();
        }

        public async Task<ActionResponse> DeleteAsync(ClaimNoteDeleteModel model)
        {
            var existNote = await _claimNoteRepository.Query().FirstOrDefaultAsync(x => x.Id == model.Id);
            if (existNote != null)
            {
                SoftDelete(existNote, model.MemberId);

                _claimNoteRepository.Update(existNote);
                await _claimNoteRepository.CommitAsync();

                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = existNote.ClaimId,
                    MemberId = model.MemberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.Delete,
                    ClaimHistoryAction = ClaimHistoryAction.ClaimNoteRemoved
                });

                return ActionResponse.SuccessResult();
            }

            return ActionResponse.FailResult(ValidationErrorMessages.NotFound(EntityNames.Note));
        }
    }
}