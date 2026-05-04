using BillingService.Domain.Extensions;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.Claims.WriteOff;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public sealed class WriteOffService : BaseService, IWriteOffService
    {
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IRepository<BillingDbContext, WriteOffReasonCodeEntity> _writeOffReasonCodeRepository;
        private readonly IRepository<BillingDbContext, ClaimWriteOffEntity> _claimWriteOffRepository;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> _claimChargeEntryWriteOffRepository;
        private readonly IRepository<BillingDbContext, ClaimNoteEntity> _claimNoteRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineEntity> _paymentClaimServiceLineRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimEntity> _paymentClaimRepository;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryEntity> _chargeEntryRepository;
        private readonly IClaimService _claimService;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentClaimService _paymentClaimService;
        private readonly IClaimHistoryService _claimHistoryService;
        private readonly IClaimManagerService _claimManagerService;
        private readonly IMessageBus _bus;


        public WriteOffService(IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IRepository<BillingDbContext, WriteOffReasonCodeEntity> writeOffReasonCodeRepository,
            IRepository<BillingDbContext, ClaimWriteOffEntity> claimWriteOffRepository,
            IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> claimChargeEntryWriteOffRepository,
            IRepository<BillingDbContext, ClaimNoteEntity> claimNoteRepository,
            IRepository<BillingDbContext, PaymentClaimServiceLineEntity> paymentClaimServiceLineRepository,
            IRepository<BillingDbContext, PaymentClaimEntity> paymentClaimRepository,
            IClaimService claimService,
            IPaymentService paymentService,
            IPaymentClaimService paymentClaimService,
            IClaimHistoryService claimHistoryService,
            IClaimManagerService claimManagerService,
            IMessageBus bus,
            IRepository<BillingDbContext, ClaimChargeEntryEntity> chargeEntryRepository)
        {
            _claimRepository = claimRepository;
            _claimWriteOffRepository = claimWriteOffRepository;
            _claimChargeEntryWriteOffRepository = claimChargeEntryWriteOffRepository;
            _claimNoteRepository = claimNoteRepository;
            _claimService = claimService;
            _paymentService = paymentService;
            _paymentClaimService = paymentClaimService;
            _claimHistoryService = claimHistoryService;
            _writeOffReasonCodeRepository = writeOffReasonCodeRepository;
            _claimManagerService = claimManagerService;
            _paymentClaimServiceLineRepository = paymentClaimServiceLineRepository;
            _paymentClaimRepository = paymentClaimRepository;
            _bus = bus;
            _chargeEntryRepository = chargeEntryRepository;
        }
        //public async Task<AddWriteOffResponseModel> AddAsync(WriteOffClaimModelWithUserInfo model)
        //{
        //    AddWriteOffResponseModel result = new()
        //    {
        //        claimIdentifiers = []
        //    };
        //    List<ClaimTransactionModel> claimTransactionData = [];

        //    foreach (var writeOffClaim in model.WriteOffClaimModels)
        //    {
        //        writeOffClaim.Amount = Math.Round(writeOffClaim.Amount, 2);
        //        //if amount is greater than 0 or user has selected remaining amount option
        //        if (writeOffClaim.Amount > 0 || writeOffClaim.AmountTypeId == 1)
        //        {
        //            var claim = await _claimRepository.GetByIdAsync(writeOffClaim.ClaimId);

        //            if (claim == null) throw new NullReferenceException($"Claim #{writeOffClaim.ClaimId} not found");

        //            var writeOffAmount = writeOffClaim.Amount;
        //            DateTime reminddate = DateTime.Now;
        //            if (!string.IsNullOrEmpty(writeOffClaim.Note))
        //            {
        //                var Note = new ClaimNoteEntity
        //                {
        //                    RemindDate = reminddate,
        //                    Note = writeOffClaim.Note,
        //                    ClaimId = claim.Id
        //                };
        //                MarkCreated(Note, model.MemberId);
        //                _claimNoteRepository.Add(Note);
        //            }

        //            GetBillingClaimDetailsModel getModel = new GetBillingClaimDetailsModel() { ClaimId = writeOffClaim.ClaimId };

        //            getModel.ChargeEntryId = writeOffClaim.IsServiceLine.Value ? writeOffClaim.ServiceLineId : null;
        //            var chargeEntries = await _claimService.GetClaimChargesForAccountAsync(getModel);
        //            if (chargeEntries.Where(x => x.BalanceAmount != 0).Count() == 0)
        //                break;
        //            List<BillingClaimDetailsModel> chargeEntriesToWriteOff = [];

        //            //to handle Evenly across sceanrio
        //            if (writeOffClaim.ApplicationTypeId == 5)
        //            {
        //                chargeEntriesToWriteOff = chargeEntries.Where(x => x.BalanceAmount > 0).ToList();
        //                var lowestBalance = chargeEntriesToWriteOff.Min(x => x.BalanceAmount);
        //                if (lowestBalance < (writeOffAmount / chargeEntriesToWriteOff.Count))
        //                {
        //                    result.errorMsg += $" {claim.ClaimIdentifier}";
        //                    continue;
        //                }
        //                else
        //                {
        //                    var claimWriteOff = new ClaimWriteOffEntity()
        //                    {
        //                        ClaimId = writeOffClaim.ClaimId,
        //                        WriteOffActionId = writeOffClaim.AmountTypeId,
        //                        WriteOffApplicationId = writeOffClaim.ApplicationTypeId ?? 7,
        //                        PercentageOrAmount = writeOffClaim.Amount
        //                    };
        //                    MarkCreated(claimWriteOff, model.MemberId);
        //                    await _claimWriteOffRepository.AddAndGetAsync(claimWriteOff);

        //                    foreach (var chargeEntry in chargeEntriesToWriteOff)
        //                    {
        //                        var writeOff = writeOffAmount / chargeEntriesToWriteOff.Count;

        //                        var claimChargeEntryWriteOff = new ClaimChargeEntryWriteOffEntity()
        //                        {
        //                            ClaimWriteOffId = claimWriteOff.Id,
        //                            WriteOffReasonCodeId = writeOffClaim.ReasonCodeId,
        //                            WriteOffReasonCodeIdOrig = writeOffClaim.ReasonCodeId,
        //                            ClaimChargeEntryId = chargeEntry.Id,
        //                            WriteOffAmount = writeOff,
        //                            WriteOffAmountOrig = writeOff
        //                        };
        //                        MarkCreated(claimChargeEntryWriteOff, model.MemberId);
        //                        _claimChargeEntryWriteOffRepository.Add(claimChargeEntryWriteOff);
        //                        await _claimChargeEntryWriteOffRepository.CommitAsync();
        //                        claimTransactionData.Add(PrepareClaimTransaction(claimChargeEntryWriteOff.Id, ClaimTransactionType.writeOff));
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                var claimWriteOff = new ClaimWriteOffEntity()
        //                {
        //                    ClaimId = writeOffClaim.ClaimId,
        //                    WriteOffActionId = writeOffClaim.AmountTypeId,
        //                    WriteOffApplicationId = writeOffClaim.ApplicationTypeId ?? 7,
        //                    PercentageOrAmount = writeOffClaim.Amount
        //                };
        //                MarkCreated(claimWriteOff, model.MemberId);
        //                await _claimWriteOffRepository.AddAndGetAsync(claimWriteOff);

        //                chargeEntriesToWriteOff = chargeEntries.Count() > 1 ? chargeEntries.OrderByWriteOffApplicationType(writeOffClaim.ApplicationTypeId)
        //                : chargeEntries.Where(x => x.BalanceAmount > 0).ToList();

        //                if (writeOffClaim.AmountTypeId == 1)
        //                {
        //                    var chargeEntriesWithNegativeBalance = chargeEntries.Where(x => x.BalanceAmount < 0).ToList();
        //                    if (chargeEntriesWithNegativeBalance.Count > 0)
        //                    {
        //                        writeOffClaim.Amount += chargeEntriesWithNegativeBalance.Sum(x => x.BalanceAmount * -1);
        //                        writeOffAmount = writeOffClaim.Amount;
        //                        claimWriteOff.PercentageOrAmount = writeOffClaim.Amount;
        //                        _claimWriteOffRepository.Update(claimWriteOff);
        //                        await _claimWriteOffRepository.CommitAsync();
        //                    }
        //                }

        //                foreach (var chargeEntry in chargeEntriesToWriteOff)
        //                {
        //                    if (writeOffAmount <= 0) break;

        //                    var balanceDifference = Math.Clamp(writeOffAmount, 0, chargeEntry.BalanceAmount);
        //                    writeOffAmount -= balanceDifference;

        //                    var claimChargeEntryWriteOff = new ClaimChargeEntryWriteOffEntity()
        //                    {
        //                        ClaimWriteOffId = claimWriteOff.Id,
        //                        WriteOffReasonCodeId = writeOffClaim.ReasonCodeId,
        //                        WriteOffReasonCodeIdOrig = writeOffClaim.ReasonCodeId,
        //                        ClaimChargeEntryId = chargeEntry.Id,
        //                        WriteOffAmount = balanceDifference,
        //                        WriteOffAmountOrig = balanceDifference
        //                    };
        //                    MarkCreated(claimChargeEntryWriteOff, model.MemberId);
        //                    _claimChargeEntryWriteOffRepository.Add(claimChargeEntryWriteOff);
        //                    await _claimChargeEntryWriteOffRepository.CommitAsync();
        //                    claimTransactionData.Add(PrepareClaimTransaction(claimChargeEntryWriteOff.Id, ClaimTransactionType.writeOff));
        //                }

        //            }
        //            //close claim if fully written off
        //            getModel.ChargeEntryId = null;
        //            chargeEntries = await _claimService.GetClaimChargesForAccountAsync(getModel);

        //            if (chargeEntries.Sum(x => x.BalanceAmount) == 0 && chargeEntries.Where(x => x.BalanceAmount < 0).Count() == 0)
        //            {
        //                await _claimManagerService.UpdateClaimStatusAsync(writeOffClaim.ClaimId, ClaimStatus.Closed,
        //                    model.AccountInfoId, false);
        //            }

        //            //await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
        //            //{
        //            //    ClaimId = writeOffClaim.ClaimId,
        //            //    MemberId = model.MemberId,
        //            //    Mode = ClaimActionMode.User,
        //            //    ClaimAction = ClaimAction.Writeoff,
        //            //    ClaimHistoryAction = ClaimHistoryAction.WriteoffWithAmount,
        //            //    NewValue = $"{writeOffClaim.Amount}",
        //            //});

        //            //await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
        //            //{
        //            //    ClaimId = writeOffClaim.ClaimId,
        //            //    MemberId = model.MemberId,
        //            //    Mode = ClaimActionMode.User,
        //            //    ClaimAction = ClaimAction.Writeoff,
        //            //    ClaimHistoryAction = ClaimHistoryAction.WriteoffWithReasonCode,
        //            //    NewValue = $"{GetWriteOffReasonDescription((WriteOffReasonDescription)writeOffClaim.ReasonCodeId)}"
        //            //});

        //            result.claimIdentifiers.Add(claim.ClaimIdentifier);
        //        }
        //    }
        //    if (claimTransactionData.Count != 0)
        //    {
        //        await _bus.SendBatchAsync(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);
        //    }
        //    return result;
        //}

        public async Task<AddWriteOffResponseModel> AddAsync(WriteOffClaimModelWithUserInfo model)
        {
            List<ClaimTransactionModel> claimTransactionData = [];
            try
            {
                if (model.Amount > 0 || model.AmountTypeId == 1) //if amount is greater than 0 or user has selected remaining amount option
                {
                    GetBillingClaimDetailsModel getModel = new() { ClaimId = model.ClaimId };
                    getModel.ChargeEntryId = model.IsServiceLine.Value ? model.ServiceLineId : null;
                    var chargeEntries = await _claimService.GetClaimChargesForAccountAsync(getModel);
                    List<BillingClaimDetailsModel> chargeEntriesToWriteOff = new();

                    model.Amount = Math.Round(model.Amount, 2);

                    var writeOffAmount = model.Amount;
                    if (!string.IsNullOrEmpty(model.Note))
                    {
                        var note = new ClaimNoteEntity
                        {
                            RemindDate = DateTime.Now,
                            Note = model.Note,
                            ClaimId = model.ClaimId
                        };
                        MarkCreated(note, model.MemberId);
                        await _claimNoteRepository.AddAsync(note);
                    }

                    // Collect all claim charge entry write-offs in a list for batch insertion
                    List<ClaimChargeEntryWriteOffEntity> claimChargeEntryWriteOffs = new();

                    //to handle Evenly across sceanrio
                    if (model.ApplicationTypeId == 5)
                    {
                        chargeEntriesToWriteOff = chargeEntries.Where(x => x.BalanceAmount > 0).ToList();
                        var lowestBalance = chargeEntriesToWriteOff.Min(x => x.BalanceAmount);
                        if (lowestBalance < (writeOffAmount / chargeEntriesToWriteOff.Count))
                        {
                            return new AddWriteOffResponseModel() { success = false, errorMsg = "Can not perform Evenly Across write off for selected claim" };
                        }
                        else
                        {
                            var claimWriteOff = new ClaimWriteOffEntity
                            {
                                ClaimId = model.ClaimId,
                                WriteOffActionId = model.AmountTypeId,
                                WriteOffApplicationId = model.ApplicationTypeId ?? 7,
                                PercentageOrAmount = model.Amount
                            };
                            MarkCreated(claimWriteOff, model.MemberId);
                            await _claimWriteOffRepository.AddAndGetAsync(claimWriteOff);

                            foreach (var chargeEntry in chargeEntriesToWriteOff)
                            {
                                var writeOff = writeOffAmount / chargeEntriesToWriteOff.Count;

                                var claimChargeEntryWriteOff = new ClaimChargeEntryWriteOffEntity()
                                {
                                    ClaimWriteOffId = claimWriteOff.Id,
                                    WriteOffReasonCodeId = model.ReasonCodeId,
                                    WriteOffReasonCodeIdOrig = model.ReasonCodeId,
                                    ClaimChargeEntryId = chargeEntry.Id,
                                    WriteOffAmount = writeOff,
                                    WriteOffAmountOrig = writeOff
                                };
                                MarkCreated(claimChargeEntryWriteOff, model.MemberId);
                                claimChargeEntryWriteOffs.Add(claimChargeEntryWriteOff);
                            }
                        }
                    }
                    else
                    {
                        var claimWriteOff = new ClaimWriteOffEntity
                        {
                            ClaimId = model.ClaimId,
                            WriteOffActionId = model.AmountTypeId,
                            WriteOffApplicationId = model.ApplicationTypeId ?? 7,
                            PercentageOrAmount = model.Amount
                        };
                        MarkCreated(claimWriteOff, model.MemberId);
                        await _claimWriteOffRepository.AddAndGetAsync(claimWriteOff);

                        chargeEntriesToWriteOff = chargeEntries.Count() > 1
                            ? chargeEntries.OrderByWriteOffApplicationType(model.ApplicationTypeId)
                            : chargeEntries.Where(x => x.BalanceAmount > 0).ToList();

                        if (model.AmountTypeId == 1)
                        {
                            var chargeEntriesWithNegativeBalance = chargeEntries.Where(x => x.BalanceAmount < 0).ToList();
                            if (chargeEntriesWithNegativeBalance.Any())
                            {
                                model.Amount += chargeEntriesWithNegativeBalance.Sum(x => x.BalanceAmount * -1);
                                writeOffAmount = model.Amount;
                                claimWriteOff.PercentageOrAmount = model.Amount;
                                _claimWriteOffRepository.Update(claimWriteOff);
                                await _claimWriteOffRepository.CommitAsync();
                            }
                        }

                        foreach (var chargeEntry in chargeEntriesToWriteOff)
                        {
                            if (writeOffAmount <= 0) break;

                            var balanceDifference = Math.Clamp(writeOffAmount, 0, chargeEntry.BalanceAmount);
                            writeOffAmount -= balanceDifference;

                            var claimChargeEntryWriteOff = new ClaimChargeEntryWriteOffEntity
                            {
                                ClaimWriteOffId = claimWriteOff.Id,
                                WriteOffReasonCodeId = model.ReasonCodeId,
                                WriteOffReasonCodeIdOrig = model.ReasonCodeId,
                                ClaimChargeEntryId = chargeEntry.Id,
                                WriteOffAmount = balanceDifference,
                                WriteOffAmountOrig = balanceDifference
                            };
                            MarkCreated(claimChargeEntryWriteOff, model.MemberId);
                            claimChargeEntryWriteOffs.Add(claimChargeEntryWriteOff);
                            claimTransactionData.Add(PrepareClaimTransaction(claimChargeEntryWriteOff.Id, ClaimTransactionType.writeOff));
                        }
                    }

                    // Batch insert all collected write-offs
                    if (claimChargeEntryWriteOffs.Count > 0)
                    {
                        await _claimChargeEntryWriteOffRepository.AddRangeAsync(claimChargeEntryWriteOffs);
                        await _claimChargeEntryWriteOffRepository.CommitAsync();
                    }

                    //close claim if fully written off
                    getModel.ChargeEntryId = null;
                    chargeEntries = await _claimService.GetClaimChargesForAccountAsync(getModel);
                    if (chargeEntries.Sum(x => x.BalanceAmount) == 0 && !chargeEntries.Any(x => x.BalanceAmount < 0))
                    {
                        await _claimManagerService.UpdateClaimStatusAsync(model.ClaimId, ClaimStatus.Closed,
                            model.AccountInfoId, false);
                    }

                    await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                    {
                        ClaimId = model.ClaimId,
                        MemberId = model.MemberId,
                        Mode = ClaimActionMode.User,
                        ClaimAction = ClaimAction.Writeoff,
                        ClaimHistoryAction = ClaimHistoryAction.WriteoffApplied,
                        ActionDate = EstDateTime,
                        NewValue = model.Amount.ToString("F2"),
                    });
                }
                if (!claimTransactionData.IsNullOrEmpty())
                {
                    await _bus.SendBatchAsync(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData.ToList());
                }
            }
            catch (Exception)
            {
                return new AddWriteOffResponseModel() { success = false };
            }

            return new AddWriteOffResponseModel() { success = true };
        }

        public async Task DeleteChargeEntryWriteOffsByChargeIdAsync(IdsWithUserInfo model)
        {
            var chargeEntryWriteOffsToDeleted = await _claimChargeEntryWriteOffRepository.Query()
               .Where(x => model.Ids.Contains(x.Id))
               .Include(x => x.ClaimWriteOff)
               .ToListAsync();
            List<ClaimHistorySaveModel> writeOffHistory = new();
            chargeEntryWriteOffsToDeleted.ForEach(wo =>
            {
                SoftDelete(wo, model.MemberId);
                writeOffHistory.Add(new ClaimHistorySaveModel
                {
                    ClaimId = wo.ClaimWriteOff.ClaimId,
                    MemberId = model.MemberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.Writeoff,
                    ActionDate = EstDateTime,
                    ClaimHistoryAction = ClaimHistoryAction.WriteoffUnapplied,
                    NewValue = wo.WriteOffAmount?.ToString("F2"),
                });
                _bus.SendAsync(PrepareClaimTransaction(wo.Id, ClaimTransactionType.writeOff), Topics.RT_Billing_ProcessClaimTxn);
            });
            _claimChargeEntryWriteOffRepository.UpdateRange(chargeEntryWriteOffsToDeleted);
            await _claimHistoryService.AddAsync(writeOffHistory);
            await _claimChargeEntryWriteOffRepository.CommitAsync();
        }

        public async Task<List<ClaimChargeEntryWriteOffModel>> GetChargeEntryWriteOffsByChargeIdAsync(GetChargeEntryWriteOffModel model)
        {
            if (model.IsServiceLineId)
            {
                var serviceLine = await _paymentClaimServiceLineRepository.Query().FirstOrDefaultAsync(x => x.Id == model.Id);
                if(serviceLine != null && serviceLine.ClaimChargeEntryId != null) model.Id = (int)serviceLine.ClaimChargeEntryId;
            }

            var reasonCodeDescriptions = await GetReasonCodesMapAsync();

            var writeOffs = await _claimChargeEntryWriteOffRepository.Query()
                .Where(x => x.ClaimChargeEntryId == model.Id && x.DateDeleted == null)
                .Select(x => new ClaimChargeEntryWriteOffModel
                {
                    Id = x.Id,
                    WriteOffAmount = x.WriteOffAmount,
                    WriteOffReasonCodeId = x.WriteOffReasonCodeId,
                    DateLastModified = x.DateLastModified,
                    Description = reasonCodeDescriptions[x.WriteOffReasonCodeId]
                }).ToListAsync();

            return writeOffs;
        }

        public async Task<List<ClaimChargeEntryWriteOffModel>> UpdateChargeEntryWriteOffsByChargeIdAsync(EditChargeEntryWriteOffModelWithUserInfo model)
        {
            var editedIds = model.WriteOffDetails.Select(a => a.ChargeEntryWriteOffId);
            var writeOffsTobeUpdated = await _claimChargeEntryWriteOffRepository.Query()
                .Where(x => editedIds.Contains(x.Id))
                .Include(x => x.ClaimWriteOff)
                .ToListAsync();

            var writeOffsBeforeUpdate = writeOffsTobeUpdated.ToDictionary(x => x.Id,
                                       x => new
                                       {
                                           ReasonCodeId = x.WriteOffReasonCodeId,
                                           Amount = x.WriteOffAmount
                                       });

            Parallel.ForEach(writeOffsTobeUpdated, wo =>
            {
                var editedWriteOff = model.WriteOffDetails.FirstOrDefault(x => x.ChargeEntryWriteOffId == wo.Id);
                wo.WriteOffAmount = editedWriteOff.WriteOffAmount;
                wo.WriteOffReasonCodeId = editedWriteOff.WriteOffReasonCodeId;

                MarkUpdated(wo, model.MemberId);
            });

            _claimChargeEntryWriteOffRepository.UpdateRange(writeOffsTobeUpdated);
            model.ClaimId = model.ClaimId <= 0 ? writeOffsTobeUpdated[0].ClaimWriteOff.ClaimId : model.ClaimId;
            await _claimChargeEntryWriteOffRepository.CommitAsync();
            List<ClaimHistoryFieldSaveModel> writeOffHistory = new();
            var reasonCodeDescriptions = await GetReasonCodesMapAsync();

            var tasks = writeOffsTobeUpdated.Select(async writeOff =>
            {
                var ActionDate = EstDateTime;
                if (writeOffsBeforeUpdate[writeOff.Id].Amount != writeOff.WriteOffAmount)
                    writeOffHistory.Add(new ClaimHistoryFieldSaveModel
                    {
                        ClaimId = model.ClaimId,
                        MemberId = model.MemberId,
                        Mode = ClaimActionMode.User,
                        ClaimAction = ClaimAction.Writeoff,
                        ClaimHistoryAction = ClaimHistoryAction.WriteOffUpdated,
                        NewValue = writeOff.WriteOffAmount?.ToString("F2"),
                        OldValue = writeOffsBeforeUpdate[writeOff.Id].Amount?.ToString("F2"),
                        ClaimHistoryField = ClaimHistoryField.Amount,
                        ActionDate = ActionDate
                    });
                if (writeOffsBeforeUpdate[writeOff.Id].ReasonCodeId != writeOff.WriteOffReasonCodeId)
                    writeOffHistory.Add(new ClaimHistoryFieldSaveModel
                    {
                        ClaimId = model.ClaimId,
                        MemberId = model.MemberId,
                        Mode = ClaimActionMode.User,
                        ClaimAction = ClaimAction.Writeoff,
                        ClaimHistoryAction = ClaimHistoryAction.WriteOffUpdated,
                        NewValue = $"{reasonCodeDescriptions[writeOff.WriteOffReasonCodeId]}",
                        OldValue = $"{reasonCodeDescriptions[writeOffsBeforeUpdate[writeOff.Id].ReasonCodeId]}",
                        ClaimHistoryField = ClaimHistoryField.ReasonCode,
                        ActionDate = ActionDate
                    });
                await _bus.SendAsync(PrepareClaimTransaction(writeOff.Id, ClaimTransactionType.writeOff), Topics.RT_Billing_ProcessClaimTxn);
            });
            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            await _claimHistoryService.AddAsync(writeOffHistory);
            return writeOffsTobeUpdated.Select(x =>
            {
                return new ClaimChargeEntryWriteOffModel
                {
                    Id = x.Id,
                    WriteOffAmount = x.WriteOffAmount ?? 0,
                    Description = x.WriteOffReasonCode.Description,
                    WriteOffReasonCodeId = x.WriteOffReasonCodeId,
                    DateLastModified = x.DateLastModified
                };
            }).ToList();
        }

        private async Task<Dictionary<int, string>> GetReasonCodesMapAsync()
        {
            return await _writeOffReasonCodeRepository.Query()
                .Where(x => x.DateDeleted == null)
                .ToDictionaryAsync(x => x.Id,
                x => x.Description);
        }

        public async Task<List<WriteOffReasonCodDescriptionModel>> GetReasonCodesAsync()
        {
            return await _writeOffReasonCodeRepository.Query()
                .Where(x => x.DateDeleted == null)
                .Select(x => new WriteOffReasonCodDescriptionModel
                {
                    Id = x.Id,
                    Description = x.Description
                }).ToListAsync();
        }


        //public async Task<string> GetWriteOffDescriptionAsync(int reasonCodeId)
        //{
        //    return await _writeOffReasonCodeRepository.Query().Where(x =>
        //            x.Id == reasonCodeId)
        //            .Select(x => x.Description)
        //            .FirstOrDefaultAsync();
        //}
    }
}
