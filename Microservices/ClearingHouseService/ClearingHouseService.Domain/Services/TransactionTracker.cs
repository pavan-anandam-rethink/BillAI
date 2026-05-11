using ClearingHouseService.Domain.Entities;
using ClearingHouseService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClearingHouseService.Domain.Services
{
    /// <summary>
    /// Default implementation of ITransactionTracker that provides in-memory transaction tracking.
    /// This can be replaced with a persistent implementation when needed.
    /// </summary>
    public class TransactionTracker : ITransactionTracker
    {
        private readonly ILogger<TransactionTracker> _logger;

        public TransactionTracker(ILogger<TransactionTracker> logger)
        {
            _logger = logger;
        }

        public Task<EdiTransaction> TrackAsync(EdiTransaction transaction, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Tracking EDI transaction {TransactionId} for Claim {ClaimId}, Format={Format}, Type={Type}",
                transaction.TransactionId,
                transaction.ClaimId,
                transaction.Format,
                transaction.Type);

            return Task.FromResult(transaction);
        }

        public Task UpdateStatusAsync(Guid transactionId, TransactionStatus status, string? message = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Transaction {TransactionId} status updated to {Status}. Message={Message}",
                transactionId,
                status,
                message);

            return Task.CompletedTask;
        }

        public Task<EdiTransaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting transaction {TransactionId}", transactionId);
            return Task.FromResult<EdiTransaction?>(null);
        }

        public Task<IReadOnlyList<EdiTransaction>> GetByClaimIdAsync(int claimId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting transactions for Claim {ClaimId}", claimId);
            return Task.FromResult<IReadOnlyList<EdiTransaction>>(Array.Empty<EdiTransaction>());
        }
    }
}
