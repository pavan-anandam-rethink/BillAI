using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentClaimServiceLine;
using BillingService.Domain.Models.PaymentClaimServiceLineAdjustment;
using BillingService.Domain.Services.Common;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Payment
{
    public class PaymentServiceLineAdjustmentService : BaseService, IPaymentServiceLineAdjustmentService
    {
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> _serviceLineAdjustmentRepository;
        private readonly IPaymentClaimService _paymentClaimService;
        private readonly IWriteOffService _writeOffService;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;

        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineEntity> _serviceLineRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimEntity> _paymentClaimRepository;
        private readonly IRepository<BillingDbContext, PaymentAdjustmentReasonEntity> _adjustmentReasonRepository;
        private IClaimHistoryService _claimHistoryService;
        private readonly IMessageBus _bus;
        private readonly ICacheService _cacheService;

        private const string adjustmentReasonCodeKey = $"Client_Adjustment_ReasonCodes";
        private const int cacheExpiration = 60;

        public PaymentServiceLineAdjustmentService(
            IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> serviceLineAdjustmentRepository,
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IRepository<BillingDbContext, PaymentClaimServiceLineEntity> serviceLineRepository,
            IRepository<BillingDbContext, PaymentAdjustmentReasonEntity> adjustmentReasonRepository,
            IRepository<BillingDbContext, PaymentClaimEntity> paymentClaimRepository,
            IWriteOffService writeOffService,
            IPaymentClaimService paymentClaimService,
            IRepository<BillingDbContext, PaymentEntity> paymentRepository,
            IMessageBus bus,
            IClaimHistoryService claimHistoryService,
            ICacheService cacheService)
        {
            _serviceLineAdjustmentRepository = serviceLineAdjustmentRepository;
            _claimRepository = claimRepository;
            _serviceLineRepository = serviceLineRepository;
            _adjustmentReasonRepository = adjustmentReasonRepository;
            _paymentClaimRepository = paymentClaimRepository;
            _writeOffService = writeOffService;
            _paymentClaimService = paymentClaimService;
            _bus = bus;
            _claimHistoryService = claimHistoryService;
            _cacheService = cacheService;
        }


        public async Task<List<PaymentClaimServiceLineAdjustmentModel>> GetPaymentServiceLineAdjustments(
            int serviceLineId)
        {
            // Step 1: Fetch from DB with basic filters and includes
            var rawAdjustments = await _serviceLineAdjustmentRepository.Query()
                .Include(x => x.PaymentClaimServiceLine)
                .Include(x => x.PaymentClaimServiceLine.PaymentClaim)
                .Where(x => x.PaymentClaimServiceLineId == serviceLineId && x.DateDeleted == null)
                .ToListAsync();

            // Step 2: In-memory filtering and projection
            var adjustments = rawAdjustments
                .Where(x => FilterPaymentClaimStatusCode(x.PaymentClaimServiceLine.PaymentClaim.ClaimStatus))
                .Select(x => new PaymentClaimServiceLineAdjustmentModel
                {
                    Id = x.Id,
                    Amount = x.AdjustmentAmount ?? 0,
                    GroupCode = x.AdjustmentGroupCode,
                    ReasonCode = x.AdjustmentReasonCode,
                    PostDate = x.DateLastModified,
                    isPositive = x.IsAdjustmentPositive,
                    PaymentId = x.PaymentClaimServiceLine.PaymentClaim.PaymentId
                })
                .ToList();

            return adjustments;
        }

        public async Task<List<PaymentClaimServiceLineAdjustmentModel>> GetPaymentServiceLineAdjustmentsByCharge(
            GetChargeDetailsModel model)
        {
            List<GetPaymentIdModel> paymentIds = new List<GetPaymentIdModel>();
            if (model.IsServiceLine)
            {
                var serviceLine = await _serviceLineRepository.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id && x.DateDeleted == null);
                paymentIds = (await _serviceLineRepository.GetAllAsync(x => x.ClaimChargeEntryId == serviceLine.ClaimChargeEntryId && x.Id != model.Id))
                    .Include(x => x.PaymentClaim)
                        .ThenInclude(x => x.Payment)
                    .OrderBy(x => x.PaymentClaim.Payment.Id)
                    .Select(sl => new GetPaymentIdModel()
                    {
                        ServiceLineId = sl.Id,
                        PaymentIdentifier = sl.PaymentClaim.Payment.PaymentIdentifier,
                        PostDate = sl.PaymentClaim.Payment.PostDate
                    }).ToList();
            }
            else
            {
                paymentIds = (await _serviceLineRepository.GetAllAsync(x => x.ClaimChargeEntryId == model.Id && x.DateDeleted == null))
                    .Include(x => x.PaymentClaim)
                        .ThenInclude(x => x.Payment)
                    .OrderBy(x => x.PaymentClaim.Payment.Id)
                    .Select(sl => new GetPaymentIdModel()
                    {
                        ServiceLineId = sl.Id,
                        PaymentIdentifier = sl.PaymentClaim.Payment.PaymentIdentifier,
                        PostDate = sl.PaymentClaim.Payment.PostDate
                    }).ToList();
            }

            var adjustments = await _serviceLineAdjustmentRepository.Query()
                    .Include(x => x.PaymentClaimServiceLine)
                        .ThenInclude(x => x.PaymentClaim)
                    .Where(x =>
                        paymentIds.Select(x => x.ServiceLineId).Contains(x.PaymentClaimServiceLineId) && x.DateDeleted == null)
                    .Select(x => new PaymentClaimServiceLineAdjustmentModel
                    {
                        Id = x.Id,
                        Amount = x.AdjustmentAmount ?? 0,
                        GroupCode = x.AdjustmentGroupCode,
                        serviceLineId = x.PaymentClaimServiceLineId,
                        ReasonCode = x.AdjustmentReasonCode,
                        isPositive = x.IsAdjustmentPositive,
                        PostDate = x.DateLastModified,
                        PaymentId = x.PaymentClaimServiceLine.PaymentClaim.PaymentId
                    })
                    .ToListAsync();

            foreach (var adjustment in adjustments)
            {
                adjustment.PaymentIdentifier = paymentIds.FirstOrDefault(p => p.ServiceLineId == adjustment.serviceLineId).PaymentIdentifier;
                adjustment.PostDate = paymentIds.FirstOrDefault(p => p.ServiceLineId == adjustment.serviceLineId).PostDate;
            }

            return adjustments;
        }

        public async Task<List<PaymentClaimServiceLineAdjustmentModel>> AddPaymentServiceLineAdjustmentsAsync(AddOrEditAdjustmentModel model)
        {
            var editAdjustmentModel = model;
            List<PaymentClaimServiceLineAdjustmentEntity> adjustmentsToBeAdded = [];
            foreach (var adjustment in model.AdjustmentDetails)
            {
                if (adjustment.AdjustmentId != null)
                {
                    PaymentClaimServiceLineAdjustmentEntity oldAdjustment = ExistingAdjustments(model, adjustment);
                    if (oldAdjustment != null)
                    {
                        var adjustmentDetail = new AdjustmentDetailsModel
                        {
                            AdjustmentId = oldAdjustment.Id,
                            GroupCode = adjustment.GroupCode,
                            ReasonCode = adjustment.ReasonCode,
                            Amount = adjustment.Amount,
                            isPositive = adjustment.isPositive
                        };

                        editAdjustmentModel.AdjustmentDetails = new List<AdjustmentDetailsModel> { adjustmentDetail };
                        await UpdateServiceLineAdjustmentsAsync(editAdjustmentModel);
                    }
                }
                else
                {
                    var entity = new PaymentClaimServiceLineAdjustmentEntity
                    {
                        AdjustmentAmount = adjustment.Amount,
                        AdjustmentAmountOrig = adjustment.Amount,
                        IsAdjustmentPositive = adjustment.isPositive,
                        AdjustmentGroupCode = adjustment.GroupCode,
                        AdjustmentGroupCodeOrig = adjustment.GroupCode,
                        AdjustmentReasonCode = adjustment.ReasonCode,
                        AdjustmentReasonCodeOrig = adjustment.ReasonCode,
                        PaymentClaimServiceLineId = model.ServiceLineId,
                        Mode = ClaimActionMode.User
                    };
                    MarkCreated(entity, model.MemberId);
                    adjustmentsToBeAdded.Add(entity);
                }
            }

            await _serviceLineAdjustmentRepository.AddRangeAsync(adjustmentsToBeAdded);
            await _serviceLineAdjustmentRepository.SaveChangesAsync();

            // Fetching payment claim associated with the service line
            var paymentClaim = await _serviceLineRepository.Query()
                .Include(x => x.PaymentClaim)
                .ThenInclude(x => x.Claim)
                .FirstOrDefaultAsync(x => x.Id == model.ServiceLineId && x.DateDeleted == null);

            if (paymentClaim == null)
                return new List<PaymentClaimServiceLineAdjustmentModel>();

            // Fetching service lines using paymentClaimId 
            var serviceLineIds = await _serviceLineRepository.Query()
                .Where(x => x.PaymentClaimId == paymentClaim.PaymentClaimId && x.DateDeleted == null)
                .Select(x => x.Id)
                .ToListAsync();

            // Calculating total adjustment amount for those service lines
            var adjustmentAmountSum = await _serviceLineAdjustmentRepository.Query()
                .Where(x => serviceLineIds.Contains(x.PaymentClaimServiceLineId) && x.DateDeleted == null)
                .SumAsync(x => x.AdjustmentAmount);

            // Closing claim if total adjustment equals total charge
            if (paymentClaim.PaymentClaim.TotalCharge == adjustmentAmountSum)
            {
                paymentClaim.PaymentClaim.Claim.ClaimStatus = ClaimStatus.Closed;
                await _claimRepository.UpdateAsync(paymentClaim.PaymentClaim.Claim);
                await _claimRepository.SaveChangesAsync();
            }

            List<ClaimHistorySaveModel> adjustmentHistory = new();
            foreach (var adjustment in adjustmentsToBeAdded)
            {
                await _bus.SendAsync(PrepareClaimTransaction(adjustment.Id, adjustment.AdjustmentGroupCode == "PR" ? ClaimTransactionType.patientResponsibility : ClaimTransactionType.adjustment),
                    Topics.RT_Billing_ProcessClaimTxn);

                AddAdjustmentHistory(adjustmentHistory, model, new AdjustmentDetailsModel
                {
                    AdjustmentId = adjustment.Id,
                    GroupCode = adjustment.AdjustmentGroupCode,
                    ReasonCode = adjustment.AdjustmentReasonCode,
                    Amount = adjustment.AdjustmentAmount ?? 0,
                    isPositive = adjustment.IsAdjustmentPositive
                }, EstDateTime);
            }
            // Update Claim Patient Responsibility Amount
            if (model.AdjustmentDetails.Any(x => x.GroupCode.Equals("PR", StringComparison.InvariantCultureIgnoreCase)))
            {
                await UpdateClaimPatientResponsibilityAsync(model.ServiceLineId);
            }

            await _claimHistoryService.AddAsync(adjustmentHistory);

            return adjustmentsToBeAdded.Select(x =>
            {
                return new PaymentClaimServiceLineAdjustmentModel
                {
                    Id = x.Id,
                    Amount = x.AdjustmentAmount ?? 0,
                    isPositive = x.IsAdjustmentPositive ?? false,
                    GroupCode = x.AdjustmentGroupCode,
                    ReasonCode = x.AdjustmentReasonCode,
                    PostDate = x.DateLastModified,
                };
            }).ToList();
        }

        private PaymentClaimServiceLineAdjustmentEntity ExistingAdjustments(AddOrEditAdjustmentModel model, AdjustmentDetailsModel adjustment)
        {
            try { 
                    return  _serviceLineAdjustmentRepository.Query().AsNoTracking()
                                   .FirstOrDefault(x => model.ServiceLineId == x.PaymentClaimServiceLineId && adjustment.AdjustmentId == x.Id);
            }
            catch (Exception ex)
            {
                return new PaymentClaimServiceLineAdjustmentEntity();
            }
        }

        private async Task UpdateClaimPatientResponsibilityAsync(int serviceLineId)
        {
            var claimId = await _serviceLineRepository.Query().Where(x => x.Id == serviceLineId)
                .Select(x => x.PaymentClaimId)
                .FirstOrDefaultAsync();
            if (claimId == null)
            {
                throw new Exception($"Service line with id {serviceLineId} has no Claim id");
            }

            var claimEntity = await _paymentClaimRepository.GetByIdAsync(claimId.Value);

            var claimPatientResponsibility = await _serviceLineRepository.Query()
                    .Where(x => x.PaymentClaimId == claimId)
                    .SelectMany(x => x.PaymentClaimServiceLineAdjustments)
                    .Where(y => y.AdjustmentGroupCode == "PR" && y.DateDeleted == null)
                    .ToListAsync();

            claimEntity.PatientRespAmount = claimPatientResponsibility.Where(x => (bool)x.IsAdjustmentPositive).Sum(x => x.AdjustmentAmount)
                - claimPatientResponsibility.Where(x => !(bool)x.IsAdjustmentPositive).Sum(x => x.AdjustmentAmount);
            _paymentClaimRepository.Update(claimEntity);
            await _paymentClaimRepository.CommitAsync();

        }

        public async Task DeleteServiceLineAdjustmentsAsync(AddOrEditAdjustmentModelForBulkPosting model)
        {
            var existingAdjustments = _serviceLineAdjustmentRepository.Query()
                .Where(x => x.PaymentClaimServiceLineId == model.ServiceLineId && x.DateDeleted == null)
                .ToList();

            var adjustmentToDelete = model.AdjustmentDetails?.Count > 0 ?
                existingAdjustments.Where(x => !model.AdjustmentDetails.Select(y => y.AdjustmentId).Contains(x.Id)).ToList()
                : existingAdjustments;

            if (adjustmentToDelete.Count > 0)
            {
                var dataForDelete = new IdsWithUserInfo
                {
                    Ids = adjustmentToDelete.Select(x => x.Id).ToArray(),
                    MemberId = model.MemberId,
                    AccountInfoId = model.AccountInfoId
                };
                await DeleteServiceLineAdjustmentsAsync(dataForDelete);
            }
        }

        public async Task DeleteServiceLineAdjustmentsAsync(IdsWithUserInfo model)
        {
            var adjustmentsToDelete = await _serviceLineAdjustmentRepository.Query()
                .Where(x => model.Ids.Contains(x.Id))
                .Include(x => x.PaymentClaimServiceLine)
                .ThenInclude(x => x.PaymentClaim)
                .ToListAsync();

            var claimId = adjustmentsToDelete[0].PaymentClaimServiceLine.PaymentClaim.ClaimId;

            List<ClaimHistorySaveModel> adjustmentHistory = new();
            foreach (var adjustment in adjustmentsToDelete)
            {
                SoftDelete(adjustment, model.MemberId);
                await _bus.SendAsync(PrepareClaimTransaction(adjustment.Id, adjustment.AdjustmentGroupCode == "PR" ? ClaimTransactionType.patientResponsibility : ClaimTransactionType.adjustment),
                    Topics.RT_Billing_ProcessClaimTxn);

                adjustmentHistory.Add(new ClaimHistorySaveModel
                {
                    ClaimId = claimId ?? 0,
                    MemberId = model.MemberId,
                    NewValue = $"{adjustment.AdjustmentAmount?.ToString("F2")}",
                    Mode = ClaimActionMode.User,
                    ClaimAction = adjustment.AdjustmentGroupCode.Equals("PR", StringComparison.InvariantCultureIgnoreCase)
                    ? ClaimAction.PatientResponsibility
                    : ClaimAction.Adjustment,
                    ClaimHistoryAction = adjustment.AdjustmentGroupCode.Equals("PR", StringComparison.InvariantCultureIgnoreCase)
                    ? ClaimHistoryAction.PatientResponsibilityUnapplied
                    : ClaimHistoryAction.AdjustmentUnapplied,
                    ActionDate = EstDateTime
                });
            }
            _serviceLineAdjustmentRepository.UpdateRange(adjustmentsToDelete);
            if (adjustmentsToDelete.Any(x => x.AdjustmentGroupCode == "PR"))
            {
                await UpdateClaimPatientResponsibilityAsync(adjustmentsToDelete[0].PaymentClaimServiceLineId);
            }
            await _claimHistoryService.AddAsync(adjustmentHistory);
            await _serviceLineAdjustmentRepository.CommitAsync();
        }

        public async Task<List<PaymentClaimServiceLineAdjustmentModel>> UpdateServiceLineAdjustmentsAsync(AddOrEditAdjustmentModel model)
        {
            var editedIds = model.AdjustmentDetails.Select(x => x.AdjustmentId);
            List<PaymentClaimServiceLineAdjustmentEntity> adjustmentsToUpdated =  AdjustmentsToUpdated(editedIds);

            var adjustmentsBeforeUpdate = adjustmentsToUpdated.ToDictionary(x => x.Id,
                                       x => new
                                       {
                                           AdjustmentReasonCode = x.AdjustmentReasonCode,
                                           AdjustmentGroupCode = x.AdjustmentGroupCode,
                                           IsAdjustmentPositive = x.IsAdjustmentPositive,
                                           Amount = x.AdjustmentAmount
                                       });

            adjustmentsToUpdated.ForEach(adj =>
            {
                var editedAdjustment = model.AdjustmentDetails.FirstOrDefault(x => x.AdjustmentId == adj.Id);
                adj.AdjustmentGroupCode = editedAdjustment.GroupCode;
                adj.AdjustmentReasonCode = editedAdjustment.ReasonCode;
                adj.AdjustmentAmount = editedAdjustment.Amount;
                adj.IsAdjustmentPositive = editedAdjustment.isPositive;
                MarkUpdated(adj, model.MemberId);
            });
            _serviceLineAdjustmentRepository.UpdateRange(adjustmentsToUpdated);

            List<ClaimHistoryFieldSaveModel> adjustmentHistory = new();
            foreach (var updatedAdjustment in adjustmentsToUpdated)
            {
                var ActionDate = EstDateTime;
                if (adjustmentsBeforeUpdate[updatedAdjustment.Id].AdjustmentReasonCode != updatedAdjustment.AdjustmentReasonCode || adjustmentsBeforeUpdate[updatedAdjustment.Id].AdjustmentGroupCode != updatedAdjustment.AdjustmentGroupCode)
                {
                    adjustmentHistory.Add(new ClaimHistoryFieldSaveModel
                    {
                        ClaimId = model.ClaimId,
                        MemberId = model.MemberId,
                        ClaimHistoryField = ClaimHistoryField.ReasonCode,
                        NewValue = $"{updatedAdjustment.AdjustmentGroupCode + "-" + updatedAdjustment.AdjustmentReasonCode}",
                        OldValue = $"{adjustmentsBeforeUpdate[updatedAdjustment.Id].AdjustmentGroupCode + "-" + adjustmentsBeforeUpdate[updatedAdjustment.Id].AdjustmentReasonCode}",
                        Mode = ClaimActionMode.User,
                        ClaimAction = adjustmentsBeforeUpdate[updatedAdjustment.Id].AdjustmentGroupCode.Equals("PR", StringComparison.InvariantCultureIgnoreCase)
                        ? ClaimAction.PatientResponsibility
                        : ClaimAction.Adjustment,
                        ClaimHistoryAction = adjustmentsBeforeUpdate[updatedAdjustment.Id].AdjustmentGroupCode.Equals("PR", StringComparison.InvariantCultureIgnoreCase)
                        ? ClaimHistoryAction.PatientResponsibilityUpdated
                        : ClaimHistoryAction.AdjustmentUpdated,
                        ActionDate = ActionDate
                    });
                }

                if (adjustmentsBeforeUpdate[updatedAdjustment.Id].Amount != updatedAdjustment.AdjustmentAmount)
                {
                    adjustmentHistory.Add(new ClaimHistoryFieldSaveModel
                    {
                        ClaimId = model.ClaimId,
                        MemberId = model.MemberId,
                        ClaimHistoryField = ClaimHistoryField.Amount,
                        NewValue = updatedAdjustment.IsAdjustmentPositive.Value
                        ? $"-{updatedAdjustment.AdjustmentAmount?.ToString("F2")}"
                        : $"{updatedAdjustment.AdjustmentAmount?.ToString("F2")}",
                        OldValue = adjustmentsBeforeUpdate[updatedAdjustment.Id].IsAdjustmentPositive.Value
                        ? $"-{adjustmentsBeforeUpdate[updatedAdjustment.Id].Amount?.ToString("F2")}"
                        : $"{adjustmentsBeforeUpdate[updatedAdjustment.Id].Amount?.ToString("F2")}",
                        Mode = ClaimActionMode.User,
                        ClaimAction = adjustmentsBeforeUpdate[updatedAdjustment.Id].AdjustmentGroupCode.Equals("PR", StringComparison.InvariantCultureIgnoreCase)
                        ? ClaimAction.PatientResponsibility
                        : ClaimAction.Adjustment,
                        ClaimHistoryAction = adjustmentsBeforeUpdate[updatedAdjustment.Id].AdjustmentGroupCode.Equals("PR", StringComparison.InvariantCultureIgnoreCase)
                        ? ClaimHistoryAction.PatientResponsibilityUpdated
                        : ClaimHistoryAction.AdjustmentUpdated,
                        ActionDate = ActionDate
                    });
                }
                await _bus.SendAsync(PrepareClaimTransaction(updatedAdjustment.Id, updatedAdjustment.AdjustmentGroupCode == "PR" ? ClaimTransactionType.patientResponsibility : ClaimTransactionType.adjustment),
                    Topics.RT_Billing_ProcessClaimTxn);
            }

            if (adjustmentsToUpdated.Any(x => x.AdjustmentGroupCode.Equals("PR", StringComparison.InvariantCultureIgnoreCase)))
            {
                await UpdateClaimPatientResponsibilityAsync(model.ServiceLineId);
            }

            await _claimHistoryService.AddAsync(adjustmentHistory);
            await _serviceLineAdjustmentRepository.CommitAsync();

            return adjustmentsToUpdated.Select(x =>
            {
                return new PaymentClaimServiceLineAdjustmentModel
                {
                    Id = x.Id,
                    Amount = x.AdjustmentAmount ?? 0,
                    isPositive = x.IsAdjustmentPositive ?? false,
                    GroupCode = x.AdjustmentGroupCode,
                    ReasonCode = x.AdjustmentReasonCode,
                    PostDate = x.DateLastModified,
                };
            }).ToList();
        }

        private  List<PaymentClaimServiceLineAdjustmentEntity> AdjustmentsToUpdated(IEnumerable<int?> editedIds)
        {
            try { 
                return _serviceLineAdjustmentRepository.Query()
                            .Where(x => editedIds.Contains(x.Id))
                            .ToList();
            }
            catch(Exception ex)
            {
                return new List<PaymentClaimServiceLineAdjustmentEntity>();
            }
        }

        public async Task ReapplyPRAdjustmentsAfterSecondaryBillingAsync(int claimId)
        {
            var paymentClaims = await _paymentClaimRepository.Query()
               .Where(x => x.ClaimId == claimId && x.DateDeleted == null)
               .Include(x => x.PaymentClaimServiceLines)
               .ThenInclude(y => y.PaymentClaimServiceLineAdjustments)
               .ToListAsync();

            var PRAdjustments = paymentClaims
               .SelectMany(pc => pc.PaymentClaimServiceLines
                   .SelectMany(sl => sl.PaymentClaimServiceLineAdjustments
                       .Where(adj => adj.AdjustmentGroupCode == "PR" && adj.DateDeleted == null)))
               .ToList();

            List<PaymentClaimServiceLineAdjustmentEntity> reversedPRAdjustments = [];

            foreach (var adjustment in PRAdjustments)
            {
                var entity = new PaymentClaimServiceLineAdjustmentEntity
                {
                    AdjustmentAmount = adjustment.AdjustmentAmount,
                    AdjustmentAmountOrig = adjustment.AdjustmentAmountOrig,
                    IsAdjustmentPositive = !adjustment.IsAdjustmentPositive,
                    AdjustmentGroupCode = adjustment.AdjustmentGroupCode,
                    AdjustmentGroupCodeOrig = adjustment.AdjustmentGroupCodeOrig,
                    AdjustmentReasonCode = adjustment.AdjustmentReasonCode,
                    AdjustmentReasonCodeOrig = adjustment.AdjustmentReasonCodeOrig,
                    PaymentClaimServiceLineId = adjustment.PaymentClaimServiceLineId,
                    Mode = ClaimActionMode.System // for PR reversal the ClaimActionMode will be system
                };

                MarkCreated(entity, 0); // As it is system generated adjustment we will store member 
                reversedPRAdjustments.Add(entity);
            }
            await _serviceLineAdjustmentRepository.AddRangeAsync(reversedPRAdjustments);
            await _serviceLineAdjustmentRepository.SaveChangesAsync();
            foreach (var adjustment in reversedPRAdjustments)
            {
                //Add Patient Responsibility History tracking
                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = claimId,
                    MemberId = 0,
                    Mode = ClaimActionMode.System,
                    ClaimAction = ClaimAction.PatientResponsibility,
                    ClaimHistoryAction = ClaimHistoryAction.PatientResponsibilityApplied,
                    NewValue = adjustment.IsAdjustmentPositive.Value ? $"-{adjustment.AdjustmentAmount?.ToString("F2")}" : $"{adjustment.AdjustmentAmount?.ToString("F2")}",
                    OldValue = $"{adjustment.AdjustmentGroupCode + "-" + adjustment.AdjustmentReasonCode}"
                });

                await _bus.SendAsync(PrepareClaimTransaction(adjustment.Id, ClaimTransactionType.patientResponsibility), Topics.RT_Billing_ProcessClaimTxn);
            }

            paymentClaims.ForEach(x =>
            {
                x.PatientRespAmount = 0; // as we are reversing all PR values Patient Responsibilty total will become zero
                _paymentClaimRepository.Update(x);
            });
            await _serviceLineAdjustmentRepository.CommitAsync();
            await _paymentClaimRepository.CommitAsync();
        }

        public async Task<List<AdjustmentReasonCodes>> GetAdjustmentReasonDescriptionsAsync(string code)
        {
            var adjustmentReasonCodes = await _cacheService.GetOrSetCacheAsync(
                adjustmentReasonCodeKey,
                async () => await _adjustmentReasonRepository.Query().ToListAsync(),
                TimeSpan.FromMinutes(cacheExpiration)
            );

            return adjustmentReasonCodes
                .Select(x => new AdjustmentReasonCodes
                {
                    ReasonCode = $"{x.GroupCode}-{x.AdjustmentCode}",
                    Description = x.Description,
                    IsDefault = x.IsDefault,
                }).ToList();
        }

        private static bool FilterPaymentClaimStatusCode(string status)
        {
            return int.TryParse(status, out int value) && value < 4;
        }
        private void AddAdjustmentHistory(List<ClaimHistorySaveModel> adjustmentHistory, AddOrEditAdjustmentModel model, AdjustmentDetailsModel adjustment, DateTime EstDateTime)
        {
            var claimAction = adjustment.GroupCode .Equals("PR", StringComparison.InvariantCultureIgnoreCase)
                ? ClaimAction.PatientResponsibility
                : ClaimAction.Adjustment;

            var claimHistoryAction = adjustment.GroupCode.Equals("PR", StringComparison.InvariantCultureIgnoreCase)
                ? ClaimHistoryAction.PatientResponsibilityApplied
                : ClaimHistoryAction.AdjustmentApplied;

            adjustmentHistory.Add(new ClaimHistorySaveModel
            {
                ClaimId = model.ClaimId,
                MemberId = model.MemberId,
                Mode = ClaimActionMode.User,
                ActionDate = EstDateTime,
                ClaimAction = claimAction,
                ClaimHistoryAction = claimHistoryAction,
                NewValue = adjustment.isPositive.Value ? $"-{adjustment.Amount.ToString("F2")}" : $"{adjustment.Amount.ToString("F2")}",
                OldValue = $" "
            });

            adjustmentHistory.Add(new ClaimHistorySaveModel
            {
                ClaimId = model.ClaimId,
                MemberId = model.MemberId,
                Mode = ClaimActionMode.User,
                ActionDate = EstDateTime,
                ClaimAction = claimAction,
                ClaimHistoryAction = claimHistoryAction,
                NewValue = $"{adjustment.GroupCode + "-" + adjustment.ReasonCode}",
                OldValue = $" "
            });
        }
    }
}