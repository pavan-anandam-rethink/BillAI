using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
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
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Threading.Tasks;
using static MongoDB.Driver.WriteConcern;

namespace BillingService.Domain.Services.Billing
{
    public class ChargeEntryService : BaseService, IChargeEntryService
    {
        private readonly IRepository<BillingDbContext, ClaimChargeEntryEntity> _chargeEntryRepository;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IRepository<BillingDbContext, ChargePaymentEntity> _chargePaymentRepository;
        private readonly IRethinkMasterDataMicroServices _rethinkMasterDataMicroServices;
        private readonly IClaimHistoryService _claimHistoryService;

        public ChargeEntryService(IRepository<BillingDbContext, ClaimChargeEntryEntity> chargeEntryRepository,
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IRepository<BillingDbContext, ChargePaymentEntity> chargePaymentRepository
            , IRethinkMasterDataMicroServices rethinkMasterDataMicroServices,
            IClaimHistoryService claimHistoryService)
        {
            _rethinkMasterDataMicroServices = rethinkMasterDataMicroServices;
            _chargeEntryRepository = chargeEntryRepository;
            _claimRepository = claimRepository;
            _chargePaymentRepository = chargePaymentRepository;
            _claimHistoryService = claimHistoryService;
        }

        public async Task<ClaimChargeEntryEntity> GetChargeEntityWithChargePaymentsAsync(int chargeEntryId,
            int claimId)
        {
            var result = await (await _chargeEntryRepository.GetAllAsync(x => x.ClaimId == claimId
                                                                        && x.Id == chargeEntryId &&
                                                                        x.DateDeleted == null))
                .Include(x => x.ChargePayments).FirstOrDefaultAsync();

            return result;
        }

        public async Task<List<ClaimChargeEntryEntity>> GetChargeEntitiesWithChargePaymentsAsync(int claimId)
        {
            var result = await (await _chargeEntryRepository.GetAllAsync(x => x.ClaimId == claimId
                                                                              && x.DateDeleted == null))
                .Include(x => x.ChargePayments).ToListAsync();

            return result;
        }
        public async Task<List<ClaimChargeEntryEntity>>GetChargeEntitiesWithChargePaymentsAsync(IEnumerable<int> claimIds)
        {
            return await _chargeEntryRepository.Query()
                .Where(x => claimIds.Contains(x.ClaimId)
                         && x.DateDeleted == null)
                .Include(x => x.ChargePayments)
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task AddChargePaymentAsync(ChargePaymentEntity entity, bool commitImmediately = true)
        {
            await _chargePaymentRepository.AddAsync(entity);

            if (commitImmediately) await _chargePaymentRepository.CommitAsync();
        }

        public async Task UpdateChargeEntryAsync(ClaimChargeEntryEntity entity, bool commitImmediately = true)
        {
            _chargeEntryRepository.Update(entity);

            if (commitImmediately) await _chargePaymentRepository.CommitAsync();
        }

        public async Task UpdateChargePaymentAsync(ChargePaymentEntity entity, bool commitImmediately = true)
        {
            _chargePaymentRepository.Update(entity);

            if (commitImmediately) await _chargePaymentRepository.CommitAsync();
        }

        public async Task<int> GetMaxChargePaymentIdAsync()
        {
            var result = await (await _chargePaymentRepository.GetAllAsync())
                .OrderByDescending(x => x.Id).FirstOrDefaultAsync();

            if (result == null) return 0;

            return result.Id;
        }

        public async Task<ChargeNoteModel> AddChargeNoteAsync(AddNoteModel model)
        {
            var chargeEntity = await _chargeEntryRepository.Query().Include(x => x.Claim)
                .FirstOrDefaultAsync(x => x.Id == model.ChargeId && x.DateDeleted == null);

            var note = new ChargeNoteModel();

            if (chargeEntity == null) throw new NullReferenceException("Charge not found");

            if (!model.NoteText.IsNullOrWhiteSpace())
            {
                chargeEntity.NoteText = model.NoteText;
                chargeEntity.NoteCreatedDate = EstDateTime;
                chargeEntity.NoteCreatedBy = model.NoteCreatedBy;

                _chargeEntryRepository.Update(chargeEntity);

                await _chargeEntryRepository.CommitAsync();

                await AddClaimHistory(chargeEntity.BillingCode, ClaimHistoryAction.ChargeEntryNoteAdded);
                await AddClaimHistory(chargeEntity.NoteText, ClaimHistoryAction.ChargeEntryNoteDescAdded);

                async Task AddClaimHistory(string newValue, ClaimHistoryAction claimHistoryAction)
                {
                    await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                    {
                        ClaimId = chargeEntity.ClaimId,
                        MemberId = chargeEntity.Claim.MemberId,
                        Mode = ClaimActionMode.User,
                        ClaimAction = ClaimAction.Added,
                        ClaimHistoryAction = claimHistoryAction,
                        NewValue = newValue,
                    });

                }

                note = new ChargeNoteModel
                {
                    NoteText = chargeEntity.NoteText,
                    NoteCreatedDate = (DateTime)chargeEntity.NoteCreatedDate
                };

                var member = await _rethinkMasterDataMicroServices.GetMemberAsync(chargeEntity.Claim.AccountInfoId, chargeEntity.NoteCreatedBy ?? 0);

                note.NoteCreatorName = member.firstName + " " + member.lastName;

            }
            return note;
        }

        public async Task DeleteChargeNoteAsync(int chargeId)
        {
            var chargeEntity = await _chargeEntryRepository.Query()
                .Include(x => x.Claim)
                .FirstOrDefaultAsync(x => x.Id == chargeId && x.DateDeleted == null);

            if (chargeEntity == null) throw new NullReferenceException("Charge not found");

            var chargeHistory = await _claimHistoryService.GetAllAsync(chargeEntity.ClaimId);

            string NoteText = chargeHistory?
                .Where(x => x.HistoryActionId == ClaimHistoryAction.ChargeEntryNoteDescAdded && x.FieldId == chargeId)
                .OrderByDescending(x => x.FieldId)
                .FirstOrDefault()?.NewValue
                 ?? chargeEntity?.NoteText;

            chargeEntity.NoteText = null;
            chargeEntity.NoteCreatedDate = null;
            chargeEntity.NoteCreatedBy = null;

            await AddClaimHistory(chargeEntity.BillingCode, ClaimHistoryAction.ChargeEntryNoteRemoved);

            if (!string.IsNullOrEmpty(NoteText))
            {
                await AddClaimHistory(NoteText, ClaimHistoryAction.ChargeEntryNoteDescRemoved);
            }

            async Task AddClaimHistory(string newValue, ClaimHistoryAction claimHistoryAction)
            {
                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = chargeEntity.ClaimId,
                    MemberId = chargeEntity.Claim.MemberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.Added,
                    ClaimHistoryAction = claimHistoryAction,
                    NewValue = newValue,
                });
            }

            _chargeEntryRepository.Update(chargeEntity);
            await _chargeEntryRepository.CommitAsync();
        }

        public async Task<List<ClaimChargeItem>> GetIdsAllOpenedPatientClaimAsync(int patientId)
        {
            var result = await (await _claimRepository.GetAllAsync(x => x.ChildProfileId == patientId
                                                                && (x.DateDeleted == null || x.isPrivatePayClaim == true)))
                .Include(x => x.ClaimChargeEntries).ThenInclude(x => x.ChargePayments)
                .Select(x =>
                new ClaimChargeItem
                {
                    ClaimId = x.Id,
                    ClaimStatus = (int)PaymentClaimStatus.ProcessedAsPrimary,
                    ChargeEntries = x.ClaimChargeEntries.Where(x => x.DateDeleted == null).Select(x =>
                        new ManualPaymentChargeEntryItem
                        {
                            Id = x.Id,
                            ClaimId = x.ClaimId,
                            Charges = x.Charges,
                            TotalAmount = x.ChargePayments.Where(x => x.DateDeleted == null).Sum(chargePayment => chargePayment.Amount),
                            ClaimChargeItems = x.ChargePayments.Where(x => x.DateDeleted == null),
                            DateOfService = x.DateOfService,
                            ServiceCode = x.BillingCode,
                            Modifier1 = x.Modifier1,
                            Modifier2 = x.Modifier2,
                            Modifier3 = x.Modifier3,
                            Modifier4 = x.Modifier4,
                            Description = x.Description
                        })
                }
                ).ToListAsync();

            return result;
        }

        public async Task<List<ClaimChargeItem>> GetAllClaimsByIdAsync(PaymentEntity payment, int[] claimIds)
        {
            var claims = await _claimRepository.Query()
                .Where(x => claimIds.Contains(x.Id) && (x.DateDeleted == null || x.isPrivatePayClaim == true))
                .Include(x => x.ClaimChargeEntries).ToListAsync();

            var result = new List<ClaimChargeItem>();

            foreach (var claim in claims)
            {
                var chargeEntries = new List<ManualPaymentChargeEntryItem>();
                foreach (var chargeEntry in claim.ClaimChargeEntries.Where(x => x.DateDeleted == null))
                {
                    chargeEntry.ChargePayments = await (await _chargePaymentRepository
                        .GetAllAsync(x => x.ChargeId == chargeEntry.Id && x.DateDeleted == null)
                        ).Where(x => x.DateDeleted == null).ToListAsync();

                    chargeEntries.Add(new ManualPaymentChargeEntryItem
                    {
                        Id = chargeEntry.Id,
                        ClaimId = chargeEntry.ClaimId,
                        Charges = chargeEntry.Charges,
                        TotalAmount = chargeEntry.ChargePayments.Sum(chargePayment => chargePayment.Amount),
                        ClaimChargeItems = chargeEntry.ChargePayments,
                        Units = chargeEntry.Units,
                        DateOfService = chargeEntry.DateOfService,
                        ServiceCode = chargeEntry.BillingCode,
                        Modifier1 = chargeEntry.Modifier1,
                        Modifier2 = chargeEntry.Modifier2,
                        Modifier3 = chargeEntry.Modifier3,
                        Modifier4 = chargeEntry.Modifier4,
                        Description = chargeEntry.Description
                    });
                }

                result.Add(new ClaimChargeItem
                {
                    ClaimId = claim.Id,
                    ClaimStatus = claim.PrimaryFunderId == payment.HcFunderId ? (int)PaymentClaimStatus.ProcessedAsPrimary :
                                    (claim.SecondaryFunderId == payment.HcFunderId ? (int)PaymentClaimStatus.ProcessedAsSecondary :
                                    (claim.TertiaryFunderId == payment.HcFunderId ? (int)PaymentClaimStatus.ProcessedAsTertiery : (int)PaymentClaimStatus.Unknown)),
                    ChargeEntries = chargeEntries,
                    PatientId = claim.ChildProfileId
                });
            }

            return result;
        }
    }
}
