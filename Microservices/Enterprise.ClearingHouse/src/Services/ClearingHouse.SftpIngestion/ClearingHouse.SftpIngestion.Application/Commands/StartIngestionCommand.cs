using ClearingHouse.SharedKernel.Domain.ValueObjects;
using MediatR;

namespace ClearingHouse.SftpIngestion.Application.Commands;

/// <summary>
/// Command to initiate an SFTP ingestion job for a specified clearinghouse.
/// </summary>
/// <param name="ClearinghouseIdentifier">The clearinghouse to poll for files.</param>
/// <param name="CorrelationId">The correlation ID for distributed tracing.</param>
/// <param name="ForceExecution">Whether to bypass the polling schedule and execute immediately.</param>
public sealed record StartIngestionCommand(
    ClearinghouseIdentifier ClearinghouseIdentifier,
    CorrelationId CorrelationId,
    bool ForceExecution = false) : IRequest<StartIngestionResult>;

/// <summary>
/// Result of starting an ingestion job.
/// </summary>
/// <param name="JobId">The created job identifier.</param>
/// <param name="Status">The initial job status description.</param>
public sealed record StartIngestionResult(Guid JobId, string Status);
