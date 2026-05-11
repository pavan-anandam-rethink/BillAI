using ClearingHouseService.Domain.Entities;
using ClearingHouseService.Domain.Interfaces;
using ClearingHouseService.Infrastructure.Transport;
using Microsoft.Extensions.Logging;

namespace ClearingHouseService.Application.Pipelines
{
    /// <summary>
    /// Pipeline that encapsulates the response processing flow:
    /// 1. Download response files from clearing house
    /// 2. Track each response transaction
    /// 3. Return files for processing by existing EDI parsers
    /// </summary>
    public class ResponseProcessingPipeline
    {
        private readonly ITransportFactory _transportFactory;
        private readonly ITransactionTracker _transactionTracker;
        private readonly ILogger<ResponseProcessingPipeline> _logger;

        public ResponseProcessingPipeline(
            ITransportFactory transportFactory,
            ITransactionTracker transactionTracker,
            ILogger<ResponseProcessingPipeline> logger)
        {
            _transportFactory = transportFactory;
            _transactionTracker = transactionTracker;
            _logger = logger;
        }

        /// <summary>
        /// Executes the response processing pipeline.
        /// </summary>
        public async Task<List<(MemoryStream Data, string FileName)>> ExecuteAsync(
            ClearingHouseConfig config,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting response processing pipeline for ClearingHouse {ClearingHouseId}",
                config.ClearingHouseId);

            var transport = _transportFactory.GetTransport(config.Type);

            // Download files
            var files = await transport.ReceiveAsync(config, cancellationToken);

            // Track each downloaded file
            foreach (var (data, fileName) in files)
            {
                var transaction = EdiTransaction.Create(
                    0, // ClaimId will be determined during parsing
                    config.ClearingHouseId,
                    DetermineEdiFormat(fileName),
                    Domain.ValueObjects.TransactionType.RemittanceAdvice);

                transaction.FileName = fileName;
                await _transactionTracker.TrackAsync(transaction, cancellationToken);
            }

            _logger.LogInformation("Response processing pipeline downloaded {Count} files from ClearingHouse {ClearingHouseId}",
                files.Count, config.ClearingHouseId);

            return files;
        }

        private static Domain.ValueObjects.EdiFormat DetermineEdiFormat(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            var name = fileName.ToUpperInvariant();

            if (name.Contains("835") || name.Contains("ERA"))
                return Domain.ValueObjects.EdiFormat.Edi835;
            if (name.Contains("999"))
                return Domain.ValueObjects.EdiFormat.Edi999;
            if (name.Contains("277"))
                return Domain.ValueObjects.EdiFormat.Edi277;
            if (name.Contains("271"))
                return Domain.ValueObjects.EdiFormat.Edi271;

            return Domain.ValueObjects.EdiFormat.Edi835; // Default
        }
    }
}
