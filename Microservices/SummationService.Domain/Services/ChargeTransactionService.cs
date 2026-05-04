using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Services;
using SummationService.Domain.Interfaces;

namespace SummationService.Domain.Services;

public class ChargeTransactionService(IRepository<BillingDbContext, ChargeTransactionEntity> chargeTransactionRepository,
    IRepository<BillingDbContext, ClaimChargeEntryEntity> claimChargeEntryRepository,
    IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> claimChargeEntryWriteOffRepository,
    IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> paymentClaimServiceLineAdjustemntRepository,
    IRepository<BillingDbContext, PaymentClaimServiceLineEntity> paymentclaimServiceLineRepository,
    IHelperService helperService) : BaseService, IChargeTransactionService
{
    public async Task<bool> AddOrUpdateChargeTransactionAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken)
    {
        int chargeEntryId = await FindChargeEntryIdByTransactionTypeIdAsync(transactionType, transactionTypeId, cancellationToken) ?? 0;
        if (chargeEntryId != 0)
        {
            ChargeTransactionEntity chargeTransaction = await PrepareChargeTransactionAsync(transactionType, chargeEntryId, cancellationToken);
            if (chargeTransaction.Id == 0)
            {
                await AddChargeTransactionAsync(chargeTransaction, cancellationToken);
            }
            else
            {
                UpdateChargeTransaction(chargeTransaction, cancellationToken);
            }
            return true;
        }
        return true;
    }

    public async Task<int?> FindChargeEntryIdByTransactionTypeIdAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken)
    {
        int? chargeEntryId = 0;
        switch (transactionType)
        {
            case ClaimTransactionType.billedAmount:
            case ClaimTransactionType.deleteCharge:
                chargeEntryId = transactionTypeId;
                break;
            case ClaimTransactionType.writeOff:
                chargeEntryId = await claimChargeEntryWriteOffRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.ClaimChargeEntryId).FirstOrDefaultAsync(cancellationToken);
                break;
            case ClaimTransactionType.deleteChargePayment:
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.patientPayment:
            case ClaimTransactionType.otherPayment:
            case ClaimTransactionType.eraReceived:
                chargeEntryId = await paymentclaimServiceLineRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.ClaimChargeEntryId).FirstOrDefaultAsync(cancellationToken);
                break;
            case ClaimTransactionType.adjustment:
            case ClaimTransactionType.patientResponsibility:
                chargeEntryId = await paymentClaimServiceLineAdjustemntRepository.Query().Where(x => x.Id == transactionTypeId).Select(x => x.PaymentClaimServiceLine.ClaimChargeEntryId).FirstOrDefaultAsync(cancellationToken);
                break;
            case ClaimTransactionType.deleteClaim://TODO:Delete Charges
            //Not applicable for charge transactions
            case ClaimTransactionType.submitClaim:
            case ClaimTransactionType.newDay:
            case ClaimTransactionType.updatePaymentSummary:
            default:
                break;
        }
        return chargeEntryId;
    }

    public async Task<ChargeTransactionEntity?> GetChargeTransactionByIdAsync(int chargeEntryId, CancellationToken cancellationToken)
    {
        return await chargeTransactionRepository.Query().Where(x => x.ChargeEntryId == chargeEntryId && x.DateDeleted == null).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<ChargeTransactionEntity?>> GetChargeTransactionsByClaimIdAsync(int claimId, CancellationToken cancellationToken)
    {
        return await chargeTransactionRepository.Query().Where(x => x.ClaimId == claimId && x.DateDeleted == null).ToListAsync(cancellationToken);
    }

    public async Task<ChargeTransactionEntity> PrepareChargeTransactionAsync(ClaimTransactionType transactionType, int chargeEntryId, CancellationToken cancellationToken)
    {
        var chargeTransaction = await GetChargeTransactionByIdAsync(chargeEntryId, cancellationToken);
        chargeTransaction ??= new ChargeTransactionEntity
        {
            ChargeEntryId = chargeEntryId,
            ClaimId = await FindClaimIdByChargeEntryIdAsync(chargeEntryId),
            DateCreated = EstDateTime
        };
        await SetTransactionTypeValue(chargeTransaction, transactionType);
        chargeTransaction.DateModified = EstDateTime;
        return chargeTransaction;
    }

    public async Task<List<ChargeTransactionEntity>> PrepareChargeTransactionRangeAsync(ClaimTransactionType transactionType, int claimId, CancellationToken cancellationToken)
    {
        var chargeTransactions = new List<ChargeTransactionEntity>();
        var claimChargeEntries = await helperService.GetChargeEntriesByClaimId(claimId);
        foreach (var item in claimChargeEntries)
        {
            var chargeTransaction = new ChargeTransactionEntity
            {
                ChargeEntryId = item.Id,
                ClaimId = claimId,
                DateCreated = EstDateTime
            };
            await SetTransactionTypeValue(chargeTransaction, transactionType);
            chargeTransaction.DateModified = EstDateTime;
            chargeTransactions.Add(chargeTransaction);
        }
        return chargeTransactions;
    }

    public async Task AddChargeTransactionAsync(ChargeTransactionEntity chargeTransactionEntity, CancellationToken cancellationToken)
    {
        await chargeTransactionRepository.AddAsync(chargeTransactionEntity);
    }

    public void UpdateChargeTransaction(ChargeTransactionEntity chargeTransactionEntity, CancellationToken cancellationToken)
    {
        chargeTransactionRepository.Update(chargeTransactionEntity);
    }

    public void UpdateChargeTransactions(List<ChargeTransactionEntity> chargeTransactionsEntity, CancellationToken cancellationToken)
    {
        chargeTransactionRepository.UpdateRange(chargeTransactionsEntity);
    }

    private async Task<ChargeTransactionEntity> SetTransactionTypeValue(ChargeTransactionEntity chargeTransaction, ClaimTransactionType transactionType)
    {
        switch (transactionType)
        {
            case ClaimTransactionType.billedAmount:
                chargeTransaction.BilledAmount = await CalculateChargeTransactionSumAsync(chargeTransaction.ChargeEntryId, transactionType);
                chargeTransaction.BilledAmount = chargeTransaction.BilledAmount >= 0 ? chargeTransaction.BilledAmount : 0;
                break;
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.eraReceived:
                chargeTransaction.InsurancePayment = await CalculateChargeTransactionSumAsync(chargeTransaction.ChargeEntryId, transactionType);
                break;
            case ClaimTransactionType.patientPayment:
                chargeTransaction.PatientPayment = await CalculateChargeTransactionSumAsync(chargeTransaction.ChargeEntryId, transactionType);
                break;
            case ClaimTransactionType.adjustment:
            case ClaimTransactionType.patientResponsibility:
                chargeTransaction.PatientResponsibility = await CalculateChargeTransactionSumAsync(chargeTransaction.ChargeEntryId, ClaimTransactionType.patientResponsibility);
                chargeTransaction.Adjustment = await CalculateChargeTransactionSumAsync(chargeTransaction.ChargeEntryId, ClaimTransactionType.adjustment);
                break;
            case ClaimTransactionType.writeOff:
                chargeTransaction.WriteOff = await CalculateChargeTransactionSumAsync(chargeTransaction.ChargeEntryId, transactionType);
                break;
            case ClaimTransactionType.otherPayment:
                chargeTransaction.OtherPayment = await CalculateChargeTransactionSumAsync(chargeTransaction.ChargeEntryId, transactionType);
                break;
            case ClaimTransactionType.deleteChargePayment:
                chargeTransaction.InsurancePayment = await CalculateChargeTransactionSumAsync(chargeTransaction.ChargeEntryId, ClaimTransactionType.insurancePayment);
                chargeTransaction.PatientPayment = await CalculateChargeTransactionSumAsync(chargeTransaction.ChargeEntryId, ClaimTransactionType.patientPayment);
                chargeTransaction.OtherPayment = await CalculateChargeTransactionSumAsync(chargeTransaction.ChargeEntryId, ClaimTransactionType.otherPayment);
                chargeTransaction.Adjustment = await CalculateChargeTransactionSumAsync(chargeTransaction.ChargeEntryId, ClaimTransactionType.adjustment);
                chargeTransaction.PatientResponsibility = await CalculateChargeTransactionSumAsync(chargeTransaction.ChargeEntryId, ClaimTransactionType.patientResponsibility);
                break;
            case ClaimTransactionType.deleteCharge:
                chargeTransaction.DateDeleted = EstDateTime;
                break;
            default:
                break;
        }
        return chargeTransaction;
    }

    private async Task<int> FindClaimIdByChargeEntryIdAsync(int chargeEntryId)
    {
        return await claimChargeEntryRepository.Query()
                                        .Where(x => x.Id == chargeEntryId && x.DateDeleted == null)
                                        .Select(x => x.ClaimId)
                                        .FirstOrDefaultAsync();
    }

    private async Task<decimal> CalculateChargeAdjustmentSumAsync(int chargeEntryId, ClaimTransactionType adjustmentTransactionType)
    {
        var adjustmentAmountList = await claimChargeEntryRepository.Query()
                                        .Where(x => x.Id == chargeEntryId && x.DateDeleted == null)
                                        .SelectMany(cc => cc.Claim.PaymentClaims
                                        .SelectMany(pc => pc.PaymentClaimServiceLines
                                        .Where(pcs => pcs.ClaimChargeEntryId == chargeEntryId && pcs.DateDeleted == null))
                                        .SelectMany(pcs => pcs.PaymentClaimServiceLineAdjustments
                                        .Where(pcsa => (IsAdjustmentTypePR(adjustmentTransactionType) ? (pcsa.AdjustmentGroupCode == prAdjustmentGroupCode) : (pcsa.AdjustmentGroupCode != prAdjustmentGroupCode)) && pcsa.DateDeleted == null)
                                        .Select(x => new { x.AdjustmentAmount, x.IsAdjustmentPositive })))
                                        .ToListAsync();
        List<Tuple<bool?, decimal?>> adjustments = new List<Tuple<bool?, decimal?>>();
        foreach (var adjustment in adjustmentAmountList)
        {
            adjustments.Add(Tuple.Create(adjustment.IsAdjustmentPositive, adjustment.AdjustmentAmount));

        }
        return CalculateOverallAdjustment(adjustments);
    }

    private async Task<decimal> CalculateChargePaymentSumAsync(int chargeEntryId, int paymentTypeId)
    {
        return (decimal)await claimChargeEntryRepository.Query()
                    .Where(cc => cc.Id == chargeEntryId && cc.DateDeleted == null)
                    .Select(cc => cc.Claim.PaymentClaims
                    .SelectMany(pc => pc.PaymentClaimServiceLines
                        .Where(pcs => pcs.ClaimChargeEntryId == chargeEntryId
                            && pcs.DateDeleted == null
                            && ((paymentTypeId == (int)Rethink.Services.Common.Enums.Billing.PaymentTypes.InsurancePayment
                            || paymentTypeId == (int)Rethink.Services.Common.Enums.Billing.PaymentTypes.ERAReceived)
                            ? (pcs.PaymentClaim.Payment.PaymentTypeId == (int)Rethink.Services.Common.Enums.Billing.PaymentTypes.InsurancePayment
                            || pcs.PaymentClaim.Payment.PaymentTypeId == (int)Rethink.Services.Common.Enums.Billing.PaymentTypes.ERAReceived)
                            : pcs.PaymentClaim.Payment.PaymentTypeId == paymentTypeId))))
                    .SelectMany(pcs => pcs)
                    .SumAsync(pcs => pcs.PaymentAmount);
    }

    private async Task<decimal> CalculateChargeTransactionSumAsync(int chargeEntryId, ClaimTransactionType transactionType)
    {
        decimal totalAmount = 0;
        switch (transactionType)
        {
            case ClaimTransactionType.billedAmount:
                totalAmount = await claimChargeEntryRepository.Query()
                                .Where(x => x.Id == chargeEntryId && x.DateDeleted == null)
                                .Select(x => x.Charges)
                                .FirstOrDefaultAsync();
                break;
            case ClaimTransactionType.writeOff:
                totalAmount = await claimChargeEntryWriteOffRepository.Query()
                                .Where(x => x.ClaimChargeEntryId == chargeEntryId && x.DateDeleted == null)
                                .Select(x => x.WriteOffAmount)
                                .SumAsync() ?? 0;
                break;
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.patientPayment:
            case ClaimTransactionType.eraReceived:
            case ClaimTransactionType.otherPayment:
                totalAmount = await CalculateChargePaymentSumAsync(chargeEntryId, FindPaymentTypeId(transactionType));
                break;
            case ClaimTransactionType.adjustment:
            case ClaimTransactionType.patientResponsibility:
                totalAmount = await CalculateChargeAdjustmentSumAsync(chargeEntryId, transactionType);
                break;
            default:
                break;
        }
        return totalAmount;
    }
}
