using ClearingHouseService.Domain.Entities;

namespace ClearingHouseService.Domain.Interfaces
{
    /// <summary>
    /// Tracks EDI transactions through their lifecycle.
    /// </summary>
    public interface ITransactionTracker
    {
        /// <summary>
        /// Records a new EDI transaction.
        /// </summary>
        Task<EdiTransaction> TrackAsync(EdiTransaction transaction, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the status of an existing transaction.
        /// </summary>
        Task UpdateStatusAsync(Guid transactionId, TransactionStatus status, string? message = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a transaction by its ID.
        /// </summary>
        Task<EdiTransaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all transactions for a specific claim.
        /// </summary>
        Task<IReadOnlyList<EdiTransaction>> GetByClaimIdAsync(int claimId, CancellationToken cancellationToken = default);
    }
}
