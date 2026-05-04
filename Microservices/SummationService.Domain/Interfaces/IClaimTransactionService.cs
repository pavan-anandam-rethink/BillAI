using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;

namespace SummationService.Domain.Interfaces;

public interface IClaimTransactionService
{
    Task<int?> FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken);
    Task<ClaimTransactionEntity?> GetClaimTransactionByIdAsync(int claimId, CancellationToken cancellationToken);
    Task<ClaimTransactionEntity> PrepareClaimTransactionAsync(ClaimTransactionType transactionType, int claimId, CancellationToken cancellationToken);
    Task AddClaimTransactionAsync(ClaimTransactionEntity claimTransactionEntity, CancellationToken cancellationToken);
    //Update includes soft-delete
    void UpdateClaimTransaction(ClaimTransactionEntity claimTransactionEntity, CancellationToken cancellationToken);
    Task<bool> AddOrUpdateClaimTransactionAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken);
}
