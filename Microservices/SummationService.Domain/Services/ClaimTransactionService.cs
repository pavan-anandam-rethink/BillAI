using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Services;
using SummationService.Domain.Interfaces;

namespace SummationService.Domain.Services;
                                                                                                                        
public class ClaimTransactionService(IRepository<BillingDbContext, ClaimTransactionEntity> claimTransactionRepository,
    IHelperService helperService,
    IRepository<BillingDbContext, PaymentClaimEntity> paymentClaimRepository,
    ILogger<ClaimTransactionService> logger) : BaseService, IClaimTransactionService
{
    private readonly ILogger<ClaimTransactionService> _logger = logger;

    public async Task<bool> AddOrUpdateClaimTransactionAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Starting AddOrUpdateClaimTransactionAsync. TransactionType={TransactionType}, TransactionTypeId={TransactionTypeId}", transactionType, transactionTypeId);

        int claimId = await FindClaimIdByTransactionTypeIdAsync(transactionType, transactionTypeId, cancellationToken) ?? 0;
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Resolved ClaimId={ClaimId} for TransactionType={TransactionType}, TransactionTypeId={TransactionTypeId}", claimId, transactionType, transactionTypeId);

        if (claimId != 0)
        {
            ClaimTransactionEntity claimTransaction = await PrepareClaimTransactionAsync(transactionType, claimId, cancellationToken);
            if (claimTransaction.Id == 0)
            {
                _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Creating new ClaimTransaction for ClaimId={ClaimId}", claimId);
                await AddClaimTransactionAsync(claimTransaction, cancellationToken);
            }
            else
            {
                _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Updating existing ClaimTransaction. ClaimTransactionId={ClaimTransactionId}, ClaimId={ClaimId}", claimTransaction.Id, claimId);
                UpdateClaimTransaction(claimTransaction, cancellationToken);
            }
            await CommitClaimTransactionAsync();
            _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Successfully completed AddOrUpdateClaimTransactionAsync. ClaimId={ClaimId}", claimId);
            return true;
        }

        _logger.LogWarning($"{nameof(ClaimTransactionService)}: " + "No ClaimId found for TransactionType={TransactionType} and TransactionTypeId={TransactionTypeId}", transactionType, transactionTypeId);
        return false;
    }

    public async Task<int?> FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Starting FindClaimIdByTransactionTypeIdAsync. TransactionType={TransactionType}, TransactionTypeId={TransactionTypeId}", transactionType, transactionTypeId);

        int? claimId = 0;
        switch (transactionType)
        {
            case ClaimTransactionType.billedAmount:
            case ClaimTransactionType.deleteCharge:
                claimId = await helperService.GetClaimIdFromChargeEntryIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.writeOff:
                claimId = await helperService.GetClaimIdFromWriteOffIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.deleteChargePayment:
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.eraReceived:
            case ClaimTransactionType.patientPayment:
            case ClaimTransactionType.otherPayment:
                claimId = await helperService.GetClaimIdFromPaymentIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.adjustment:
            case ClaimTransactionType.patientResponsibility:
                claimId = await helperService.GetClaimIdFromAdjustmentIdAsync(transactionTypeId, cancellationToken);
                break;
            case ClaimTransactionType.deleteClaim:
                claimId = transactionTypeId;
                break;
            //Not applicable for claim transactions
            case ClaimTransactionType.submitClaim:
            case ClaimTransactionType.newDay:
            case ClaimTransactionType.updatePaymentSummary:
            default:
                break;
        }

        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Completed FindClaimIdByTransactionTypeIdAsync. TransactionType={TransactionType}, TransactionTypeId={TransactionTypeId}, ClaimId={ClaimId}", transactionType, transactionTypeId, claimId);
        return claimId;
    }

    private async Task CommitClaimTransactionAsync()
    {
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Committing claim transaction repository changes.");
        await claimTransactionRepository.CommitAsync();
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Commit completed.");
    }

    public async Task<ClaimTransactionEntity?> GetClaimTransactionByIdAsync(int claimId, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Querying ClaimTransaction by ClaimId={ClaimId}", claimId);
        var entity = await claimTransactionRepository.Query().Where(x => x.ClaimId == claimId && x.DateDeleted == null).FirstOrDefaultAsync(cancellationToken);
        _logger.LogInformation(entity == null ? $"{nameof(ClaimTransactionService)}: " + "No ClaimTransaction found for ClaimId={ClaimId}" : $"{nameof(ClaimTransactionService)}: " + "Found ClaimTransaction Id={ClaimTransactionId} for ClaimId={ClaimId}", entity?.Id, claimId);
        return entity;
    }

    public async Task<ClaimTransactionEntity> PrepareClaimTransactionAsync(ClaimTransactionType transactionType, int claimId, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Preparing ClaimTransaction. ClaimId={ClaimId}, TransactionType={TransactionType}", claimId, transactionType);
        var claimTransaction = await GetClaimTransactionByIdAsync(claimId, cancellationToken);
        if (claimTransaction == null)
        {
            _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "No existing ClaimTransaction found. Creating new for ClaimId={ClaimId}", claimId);
        }
        claimTransaction ??= new ClaimTransactionEntity
        {
            ClaimId = claimId,
            DateCreated = EstDateTime
        };
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Setting transaction type values for ClaimId={ClaimId}, TransactionType={TransactionType}", claimId, transactionType);
        await SetTransactionTypeValue(claimTransaction, transactionType);
        claimTransaction.DateModified = EstDateTime;
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Prepared ClaimTransaction. ClaimTransactionId={ClaimTransactionId}, ClaimId={ClaimId}", claimTransaction.Id, claimId);
        return claimTransaction;
    }

    public async Task AddClaimTransactionAsync(ClaimTransactionEntity claimTransactionEntity, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Adding ClaimTransaction for ClaimId={ClaimId}", claimTransactionEntity.ClaimId);
        await claimTransactionRepository.AddAsync(claimTransactionEntity);
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "AddClaimTransactionAsync complete for ClaimId={ClaimId}", claimTransactionEntity.ClaimId);
    }

    public void UpdateClaimTransaction(ClaimTransactionEntity claimTransactionEntity, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Updating ClaimTransaction. ClaimTransactionId={ClaimTransactionId}, ClaimId={ClaimId}", claimTransactionEntity.Id, claimTransactionEntity.ClaimId);
        claimTransactionRepository.Update(claimTransactionEntity);
    }

    private async Task<ClaimTransactionEntity> SetTransactionTypeValue(ClaimTransactionEntity claimTransaction, ClaimTransactionType transactionType)
    {
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Setting transaction values for ClaimTransactionId={ClaimTransactionId}, ClaimId={ClaimId}, TransactionType={TransactionType}", claimTransaction.Id, claimTransaction.ClaimId, transactionType);
        switch (transactionType)
        {
            case ClaimTransactionType.billedAmount:
                claimTransaction.BilledAmount = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, transactionType);
                break;
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.eraReceived:
                claimTransaction.InsurancePayment = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, transactionType);
                break;
            case ClaimTransactionType.patientPayment:
                claimTransaction.PatientPayment = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, transactionType);
                break;
            case ClaimTransactionType.otherPayment:
                claimTransaction.OtherPayment = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, transactionType);
                break;
            case ClaimTransactionType.adjustment:
            case ClaimTransactionType.patientResponsibility:
                claimTransaction.Adjustment = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, ClaimTransactionType.adjustment);
                claimTransaction.PatientResponsibility = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, ClaimTransactionType.patientResponsibility);
                break;
            case ClaimTransactionType.writeOff:
                claimTransaction.WriteOff = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, transactionType);
                break;
            case ClaimTransactionType.deleteCharge:
                claimTransaction.BilledAmount = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, ClaimTransactionType.billedAmount);
                claimTransaction.InsurancePayment = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, ClaimTransactionType.insurancePayment);
                claimTransaction.PatientPayment = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, ClaimTransactionType.patientPayment);
                claimTransaction.OtherPayment = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, ClaimTransactionType.otherPayment);
                claimTransaction.Adjustment = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, ClaimTransactionType.adjustment);
                claimTransaction.PatientResponsibility = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, ClaimTransactionType.patientResponsibility);
                claimTransaction.WriteOff = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, ClaimTransactionType.writeOff);
                break;
            case ClaimTransactionType.deleteChargePayment:
                claimTransaction.InsurancePayment = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, ClaimTransactionType.insurancePayment);
                claimTransaction.PatientPayment = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, ClaimTransactionType.patientPayment);
                claimTransaction.OtherPayment = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, ClaimTransactionType.otherPayment);
                claimTransaction.Adjustment = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, ClaimTransactionType.adjustment);
                claimTransaction.PatientResponsibility = await CalculateClaimTransactionSumAsync(claimTransaction.ClaimId, ClaimTransactionType.patientResponsibility);
                break;
            case ClaimTransactionType.deleteClaim:
                claimTransaction.DateDeleted = EstDateTime;
                break;
            default:
                _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "No transaction value changes for TransactionType={TransactionType} on ClaimId={ClaimId}", transactionType, claimTransaction.ClaimId);
                break;
        }
        return claimTransaction;
    }

    private async Task<decimal> CalculateClaimAdjustmentSumAsync(int claimId, ClaimTransactionType adjustmentTransactionType)
    {
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Calculating claim adjustment sum. ClaimId={ClaimId}, AdjustmentType={AdjustmentType}", claimId, adjustmentTransactionType);
        var adjustmentAmountList = await helperService.GetAdjustmentsFromClaimIdAsync(claimId, adjustmentTransactionType);
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Retrieved {Count} adjustment entries for ClaimId={ClaimId}", adjustmentAmountList?.Count ?? 0, claimId);

        var overall = CalculateOverallAdjustment(adjustmentAmountList);
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Calculated overall adjustment for ClaimId={ClaimId}: {Overall}", claimId, overall);
        return overall;
    }

    private async Task<decimal> CalculateClaimTransactionSumAsync(int claimId, ClaimTransactionType transactionType)
    {
        _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "Calculating claim transaction sum. ClaimId={ClaimId}, TransactionType={TransactionType}", claimId, transactionType);
        decimal totalAmount = 0;
        switch (transactionType)
        {
            case ClaimTransactionType.billedAmount:
                totalAmount = await helperService.GetBilledAmountByClaimIdAsync(claimId);
                 break;
            case ClaimTransactionType.writeOff:
                totalAmount = await helperService.CalculateClaimWriteOffSumAsync(claimId);
                break;
            case ClaimTransactionType.insurancePayment:
            case ClaimTransactionType.eraReceived:
            case ClaimTransactionType.patientPayment:
            case ClaimTransactionType.otherPayment:
                int paymentTypeId = FindPaymentTypeId(transactionType);
                totalAmount = await helperService.CalculateClaimPaymentSumAsync(claimId, paymentTypeId);
                break;
            case ClaimTransactionType.adjustment:
            case ClaimTransactionType.patientResponsibility:
                totalAmount = await CalculateClaimAdjustmentSumAsync(claimId, transactionType);
                 break;
            default:
                _logger.LogInformation($"{nameof(ClaimTransactionService)}: " + "No calculation performed for TransactionType={TransactionType} on ClaimId={ClaimId}", transactionType, claimId);
                break;
        }
        return totalAmount;
    }
}
