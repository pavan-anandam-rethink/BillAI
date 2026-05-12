using ClearingHouse.SharedKernel.Domain.ValueObjects;
using ClearingHouse.SftpIngestion.Domain.ValueObjects;
using MediatR;

namespace ClearingHouse.SftpIngestion.Application.Commands;

/// <summary>
/// Command to process a single file discovered on an SFTP endpoint.
/// Downloads the file, validates integrity, uploads to blob storage, and publishes events.
/// </summary>
/// <param name="FileName">The name of the file to process.</param>
/// <param name="FileSize">The size of the file in bytes.</param>
/// <param name="ClearinghouseIdentifier">The clearinghouse the file belongs to.</param>
/// <param name="CorrelationId">The correlation ID for distributed tracing.</param>
/// <param name="SftpConnectionDetails">The SFTP connection details for downloading.</param>
/// <param name="IngestionJobId">The parent ingestion job ID.</param>
public sealed record ProcessDiscoveredFileCommand(
    string FileName,
    long FileSize,
    ClearinghouseIdentifier ClearinghouseIdentifier,
    CorrelationId CorrelationId,
    SftpConnectionDetails SftpConnectionDetails,
    Guid IngestionJobId) : IRequest<ProcessDiscoveredFileResult>;

/// <summary>
/// Result of processing a discovered file.
/// </summary>
/// <param name="Success">Whether the file was processed successfully.</param>
/// <param name="FileReference">The blob storage reference if successful.</param>
/// <param name="ContentHash">The computed content hash.</param>
/// <param name="ErrorMessage">Error message if processing failed.</param>
public sealed record ProcessDiscoveredFileResult(
    bool Success,
    FileReference? FileReference = null,
    string? ContentHash = null,
    string? ErrorMessage = null);
