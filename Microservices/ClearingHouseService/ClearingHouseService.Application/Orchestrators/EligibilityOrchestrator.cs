using ClearingHouseService.Application.DTOs;
using ClearingHouseService.Domain.Entities;
using ClearingHouseService.Domain.Interfaces;
using ClearingHouseService.Domain.ValueObjects;
using ClearingHouseService.Infrastructure.Transport;
using Microsoft.Extensions.Logging;

namespace ClearingHouseService.Application.Orchestrators
{
    /// <summary>
    /// Default implementation of IEligibilityOrchestrator.
    /// Coordinates the 270/271 eligibility workflow using domain services and infrastructure.
    /// </summary>
    public class EligibilityOrchestrator : IEligibilityOrchestrator
    {
        private readonly ITransportFactory _transportFactory;
        private readonly ITransactionTracker _transactionTracker;
        private readonly ILogger<EligibilityOrchestrator> _logger;

        public EligibilityOrchestrator(
            ITransportFactory transportFactory,
            ITransactionTracker transactionTracker,
            ILogger<EligibilityOrchestrator> logger)
        {
            _transportFactory = transportFactory;
            _transactionTracker = transactionTracker;
            _logger = logger;
        }

        public async Task<EligibilityResult> CheckEligibilityAsync(
            string edi270Data,
            int clearingHouseId,
            CancellationToken cancellationToken = default)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation(
                "Starting eligibility check. ClearingHouseId={ClearingHouseId}, CorrelationId={CorrelationId}",
                clearingHouseId, correlationId);

            // Track the transaction
            var transaction = EdiTransaction.Create(0, clearingHouseId, EdiFormat.Edi270, TransactionType.EligibilityInquiry);
            transaction.CorrelationId = correlationId;
            transaction.EdiContent = edi270Data;
            await _transactionTracker.TrackAsync(transaction, cancellationToken);

            try
            {
                // Use API transport for eligibility (Stedi)
                var transport = _transportFactory.GetTransport(TransportProtocol.Api);

                var config = new ClearingHouseConfig
                {
                    ClearingHouseId = clearingHouseId,
                    Title = "Stedi",
                    Type = ClearingHouseType.Stedi
                };

                var fileName = $"eligibility_270_{DateTime.UtcNow:yyyyMMddHHmmss}.edi";

                await _transactionTracker.UpdateStatusAsync(transaction.TransactionId, TransactionStatus.Submitted, cancellationToken: cancellationToken);

                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(edi270Data));
                var result = await transport.SendAsync(config, fileName, stream, cancellationToken);

                if (result.IsSuccess)
                {
                    await _transactionTracker.UpdateStatusAsync(transaction.TransactionId, TransactionStatus.Completed, cancellationToken: cancellationToken);
                    _logger.LogInformation(
                        "Eligibility check completed successfully. CorrelationId={CorrelationId}", correlationId);
                    return EligibilityResult.Success(result.FileName ?? string.Empty, transaction.TransactionId);
                }

                await _transactionTracker.UpdateStatusAsync(transaction.TransactionId, TransactionStatus.Failed, result.ErrorMessage, cancellationToken);
                _logger.LogWarning(
                    "Eligibility check failed. Error={Error}, CorrelationId={CorrelationId}",
                    result.ErrorMessage, correlationId);
                return EligibilityResult.Fail(result.ErrorMessage ?? "Unknown error");
            }
            catch (Exception ex)
            {
                await _transactionTracker.UpdateStatusAsync(transaction.TransactionId, TransactionStatus.Failed, ex.Message, cancellationToken);
                _logger.LogError(ex,
                    "Eligibility check failed with exception. CorrelationId={CorrelationId}", correlationId);
                return EligibilityResult.Fail(ex.Message);
            }
        }
    }
}
