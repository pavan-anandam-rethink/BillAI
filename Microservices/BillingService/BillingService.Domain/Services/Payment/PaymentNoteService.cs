using AutoMapper;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.PaymentPosting;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Payment
{
    public class PaymentNoteService : BaseService, IPaymentNoteService
    {
        private readonly IRepository<BillingDbContext, PaymentNoteEntity> _paymentNoteRepository;
        private readonly IRepository<BillingDbContext, PaymentEntity> _paymentRepository;
        private readonly IMapper _mapper;
        private readonly IRethinkMasterDataMicroServices _rethinkService;

        public PaymentNoteService(
            IRepository<BillingDbContext, PaymentNoteEntity> paymentNoteRepository,
            IMapper mapper,
            IRepository<BillingDbContext, PaymentEntity> paymentRepository,
            IRethinkMasterDataMicroServices rethinkService
            )
        {
            _rethinkService = rethinkService;
            _paymentRepository = paymentRepository;
            _paymentNoteRepository = paymentNoteRepository;
            _mapper = mapper;
        }

        public async Task<List<PaymentNote>> GetAll(int paymentId)
        {
            var notes = await _paymentNoteRepository.Query()
                .Where(x => x.PaymentId == paymentId)
                .Select(x => new PaymentNote
                {
                    Id = x.Id,
                    PaymentId = x.PaymentId,
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

            //new request because of different dbs
            var memberNotesIdList = new List<int?>();
            notes.ForEach(note =>
            {
                memberNotesIdList.Add(note.CreatedBy);
                memberNotesIdList.Add(note.ModifiedBy);
            });
            var memberNoteIdListDistinct = memberNotesIdList.Distinct();

            var accountInfoId = await _paymentRepository.Query().Where(x => x.Id == paymentId).Select(x => x.AccountInfoId).FirstOrDefaultAsync();
            var member = await _rethinkService.GetMemberListAsync(accountInfoId ?? 0);

            var members = member.data.Where(x => memberNoteIdListDistinct.Any(id => id == x.id)).ToList();

            notes.ForEach(note =>
            {
                var creator = members.FirstOrDefault(member => member.id == note.CreatedBy);
                var modifier = members.FirstOrDefault(member => member.id == note.ModifiedBy);

                note.CreatedByName = creator?.firstName + " " + creator?.lastName;
                note.ModifiedByName = modifier?.firstName + " " + modifier?.lastName;
            });

            return notes.ToList();
        }

        public async Task<int> AddNote(PaymentNoteSaveModel model)
        {
            var newNote = new PaymentNoteEntity
            {
                RemindDate = model.RemindDate,
                Note = model.Note,
                PaymentId = model.PaymentId
            };
            MarkCreated(newNote, model.MemberId);
            await _paymentNoteRepository.AddAsync(newNote);
            await _paymentNoteRepository.CommitAsync();

            return newNote.Id;
        }

        public async Task<int> AddToPaymentsAsync(PaymentNoteSaveModel[] model)
        {
            var paymentNotes = new List<PaymentNoteEntity>();
            foreach (var paymentNoteModel in model)
            {
                var newNote = new PaymentNoteEntity
                {
                    RemindDate = paymentNoteModel.RemindDate,
                    Note = paymentNoteModel.Note,
                    PaymentId = paymentNoteModel.PaymentId
                };

                MarkCreated(newNote, model[0].MemberId);
                paymentNotes.Add(newNote);
            }

            await _paymentNoteRepository.AddRangeAsync(paymentNotes);
            await _paymentNoteRepository.CommitAsync();

            return 1;
        }

        public async Task<int> DeleteNote(PaymentNoteDeleteModel model)
        {
            var existNote = await _paymentNoteRepository.Query().FirstOrDefaultAsync(x => x.Id == model.Id);
            if (existNote != null)
            {
                SoftDelete(existNote, model.MemberId);
                _paymentNoteRepository.Update(existNote);
                await _paymentNoteRepository.CommitAsync();
                return existNote.Id;
            }

            return 0;
        }
    }
}