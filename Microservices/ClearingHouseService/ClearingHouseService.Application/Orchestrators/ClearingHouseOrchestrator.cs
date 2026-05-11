using ClearingHouseService.Application.DTOs;
using ClearingHouseService.Domain.Entities;
using ClearingHouseService.Domain.Interfaces;
using ClearingHouseService.Domain.ValueObjects;
using ClearingHouseService.Infrastructure.Transport;
using Microsoft.Extensions.Logging;

namespace ClearingHouseService.Application.Orchestrators
{
    /// <summary>
    /// Default implementation of IClearingHouseOrchestrator.
    /// Coordinates the multi-step clearing house workflows using domain services and infrastructure.
    /// </summary>
    public class ClearingHouseOrchestrator : IClearingHouseOrchestrator
    {
        private readonly ITransportFactory _transportFactory;
        private readonly ITransactionTracker _transactionTracker;
        private readonly ILogger<ClearingHouseOrchestrator> _logger;

        public ClearingHouseOrchestrator(
            ITransportFactory transportFactory,
            ITransactionTracker transactionTracker,
            ILogger<ClearingHouseOrchestrator> logger)
        {
            _transportFactory = transportFactory;
            _transactionTracker = transactionTracker;
            _logger = logger;
        }

        public async Task<ClaimSubmissionResult> SubmitClaimAsync(
            int claimId,
            string ediData,
            int clearingHouseId,
            CancellationToken cancellationToken = default)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogInformation(
                "Starting claim submission workflow. ClaimId={ClaimId}, ClearingHouseId={ClearingHouseId}, CorrelationId={CorrelationId}",
                claimId, clearingHouseId, correlationId);

            // Create and track the transaction
            var transaction = EdiTransaction.Create(claimId, clearingHouseId, EdiFormat.Edi837P, TransactionType.ClaimSubmission);
            transaction.CorrelationId = correlationId;
            transaction.EdiContent = ediData;
            await _transactionTracker.TrackAsync(transaction, cancellationToken);

            try
            {
                // Resolve the clearing house type and get the appropriate transport
                var chType = ResolveClearingHouseType(clearingHouseId);
                var transport = _transportFactory.GetTransport(chType);

                // Create the clearing house config
                var config = new ClearingHouseConfig
                {
                    ClearingHouseId = clearingHouseId,
                    Title = chType.Name,
                    Type = chType
                };

                // Generate file name
                var fileName = $"claim_{claimId}_{DateTime.UtcNow:yyyyMMddHHmmss}.edi";

                // Upload via transport
                await _transactionTracker.UpdateStatusAsync(transaction.TransactionId, TransactionStatus.Submitted, cancellationToken: cancellationToken);

                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(ediData));
                var result = await transport.SendAsync(config, fileName, stream, cancellationToken);

                if (result.IsSuccess)
                {
                    await _transactionTracker.UpdateStatusAsync(transaction.TransactionId, TransactionStatus.Completed, cancellationToken: cancellationToken);
                    _logger.LogInformation(
                        "Claim submission completed successfully. ClaimId={ClaimId}, FileName={FileName}, CorrelationId={CorrelationId}",
                        claimId, fileName, correlationId);

                    return ClaimSubmissionResult.Success(fileName, transaction.TransactionId, correlationId);
                }

                await _transactionTracker.UpdateStatusAsync(transaction.TransactionId, TransactionStatus.Failed, result.ErrorMessage, cancellationToken);
                _logger.LogWarning(
                    "Claim submission failed. ClaimId={ClaimId}, Error={Error}, CorrelationId={CorrelationId}",
                    claimId, result.ErrorMessage, correlationId);

                return ClaimSubmissionResult.Fail(result.ErrorType, result.ErrorMessage ?? "Unknown error");
            }
            catch (Exception ex)
            {
                await _transactionTracker.UpdateStatusAsync(transaction.TransactionId, TransactionStatus.Failed, ex.Message, cancellationToken);
                _logger.LogError(ex,
                    "Claim submission failed with exception. ClaimId={ClaimId}, CorrelationId={CorrelationId}",
                    claimId, correlationId);

                return ClaimSubmissionResult.Fail(TransmissionErrorType.Unknown, ex.Message);
            }
        }

        public async Task<List<(MemoryStream Data, string FileName)>> DownloadResponsesAsync(
            int clearingHouseId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting response download for ClearingHouseId={ClearingHouseId}", clearingHouseId);

            var chType = ResolveClearingHouseType(clearingHouseId);
            var transport = _transportFactory.GetTransport(chType);

            var config = new ClearingHouseConfig
            {
                ClearingHouseId = clearingHouseId,
                Title = chType.Name,
                Type = chType
            };

            var files = await transport.ReceiveAsync(config, cancellationToken);
            _logger.LogInformation("Downloaded {FileCount} response files from {ClearingHouse}", files.Count, chType.Name);

            return files;
        }

        public async Task<List<TransmissionResult>> ValidateAllConnectionsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating connections to all clearing houses");
            var results = new List<TransmissionResult>();

            var clearingHouseTypes = new[] { ClearingHouseType.Stedi, ClearingHouseType.Availity };

            foreach (var chType in clearingHouseTypes)
            {
                var transport = _transportFactory.GetTransport(chType);
                var config = new ClearingHouseConfig
                {
                    Title = chType.Name,
                    Type = chType
                };

                var result = await transport.ValidateConnectionAsync(config, cancellationToken);
                results.Add(result);
            }

            return results;
        }

        private static ClearingHouseType ResolveClearingHouseType(int clearingHouseId)
        {
            // Map clearing house IDs to types based on the existing BillingClearingHousesEnum
            return clearingHouseId switch
            {
                1 => ClearingHouseType.Stedi,   // BillingClearingHousesEnum.Stedi
                2 => ClearingHouseType.Availity, // BillingClearingHousesEnum.Availity
                _ => ClearingHouseType.Stedi     // Default to Stedi
            };
        }
    }
}
