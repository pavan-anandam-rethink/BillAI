using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;

namespace SummationService.Domain.Interfaces;

public interface IChargeTransactionService
{
    Task<int?> FindChargeEntryIdByTransactionTypeIdAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken);
    Task<ChargeTransactionEntity?> GetChargeTransactionByIdAsync(int chargeEntryId, CancellationToken cancellationToken);
    Task<ChargeTransactionEntity> PrepareChargeTransactionAsync(ClaimTransactionType transactionType, int chargeEntryId, CancellationToken cancellationToken);
    Task AddChargeTransactionAsync(ChargeTransactionEntity chargeTransactionEntity, CancellationToken cancellationToken);
    //Update includes soft-delete
    void UpdateChargeTransaction(ChargeTransactionEntity chargeTransactionEntity, CancellationToken cancellationToken);
    Task<bool> AddOrUpdateChargeTransactionAsync(ClaimTransactionType transactionType, int transactionTypeId, CancellationToken cancellationToken);
}
