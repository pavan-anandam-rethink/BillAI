using ClearingHouseService.Domain.Entities;
using ClearingHouseService.Domain.Interfaces;
using ClearingHouseService.Infrastructure.Clients;
using ClearingHouseService.Infrastructure.Transport;
using Microsoft.Extensions.Logging;

namespace ClearingHouseService.Application.Pipelines
{
    /// <summary>
    /// Pipeline that encapsulates the full claim submission flow:
    /// 1. Validate EDI content
    /// 2. Track the transaction
    /// 3. Send via transport
    /// 4. Update tracking status
    /// 5. Report to billing service
    /// </summary>
    public class ClaimSubmissionPipeline
    {
        private readonly ITransportFactory _transportFactory;
        private readonly ITransactionTracker _transactionTracker;
        private readonly IEdiValidator _ediValidator;
        private readonly ILogger<ClaimSubmissionPipeline> _logger;

        public ClaimSubmissionPipeline(
            ITransportFactory transportFactory,
            ITransactionTracker transactionTracker,
            IEdiValidator ediValidator,
            ILogger<ClaimSubmissionPipeline> logger)
        {
            _transportFactory = transportFactory;
            _transactionTracker = transactionTracker;
            _ediValidator = ediValidator;
            _logger = logger;
        }

        /// <summary>
        /// Executes the claim submission pipeline.
        /// </summary>
        public async Task<TransmissionResult> ExecuteAsync(
            ClearingHouseConfig config,
            string ediData,
            int claimId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting claim submission pipeline for Claim {ClaimId}", claimId);

            // Step 1: Validate EDI
            var validation = await _ediValidator.ValidateAsync(ediData, cancellationToken);
            if (!validation.IsValid)
            {
                _logger.LogWarning("EDI validation failed for Claim {ClaimId}: {Errors}",
                    claimId, string.Join(", ", validation.Errors));
                return TransmissionResult.Fail(
                    TransmissionErrorType.ValidationFailed,
                    string.Join("; ", validation.Errors));
            }

            // Step 2: Track
            var transaction = EdiTransaction.Create(
                claimId,
                config.ClearingHouseId,
                Domain.ValueObjects.EdiFormat.Edi837P,
                Domain.ValueObjects.TransactionType.ClaimSubmission);
            await _transactionTracker.TrackAsync(transaction, cancellationToken);

            // Step 3: Send
            var transport = _transportFactory.GetTransport(config.Type);
            var fileName = $"claim_{claimId}_{DateTime.UtcNow:yyyyMMddHHmmss}.edi";

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ediData));
            var result = await transport.SendAsync(config, fileName, stream, cancellationToken);

            // Step 4: Update tracking
            if (result.IsSuccess)
            {
                await _transactionTracker.UpdateStatusAsync(
                    transaction.TransactionId, TransactionStatus.Completed, cancellationToken: cancellationToken);
            }
            else
            {
                await _transactionTracker.UpdateStatusAsync(
                    transaction.TransactionId, TransactionStatus.Failed, result.ErrorMessage, cancellationToken);
            }

            _logger.LogInformation("Claim submission pipeline completed for Claim {ClaimId}. Success={Success}",
                claimId, result.IsSuccess);

            return result;
        }
    }
}
