using System.Diagnostics;
using System.Security.Cryptography;
using ClearingHouse.SharedKernel.Domain.Events;
using ClearingHouse.SharedKernel.Domain.Interfaces;
using ClearingHouse.SharedKernel.Domain.ValueObjects;
using ClearingHouse.SftpIngestion.Application.Commands;
using ClearingHouse.SftpIngestion.Domain.Entities;
using ClearingHouse.SftpIngestion.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ClearingHouse.SftpIngestion.Application.Handlers;

/// <summary>
/// Handles the <see cref="ProcessDiscoveredFileCommand"/> by downloading, hashing, uploading, and publishing events.
/// Uses stream-based processing to avoid loading entire files into memory.
/// </summary>
public sealed class ProcessDiscoveredFileCommandHandler : IRequestHandler<ProcessDiscoveredFileCommand, ProcessDiscoveredFileResult>
{
    private static readonly ActivitySource ActivitySource = new("ClearingHouse.SftpIngestion");

    private readonly ILogger<ProcessDiscoveredFileCommandHandler> _logger;
    private readonly ISftpConnectionPool _connectionPool;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IRepository<SftpIngestionJob> _repository;

    private readonly ResiliencePipeline _retryPipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessDiscoveredFileCommandHandler"/> class.
    /// </summary>
    public ProcessDiscoveredFileCommandHandler(
        ILogger<ProcessDiscoveredFileCommandHandler> logger,
        ISftpConnectionPool connectionPool,
        IBlobStorageService blobStorageService,
        IEventPublisher eventPublisher,
        IRepository<SftpIngestionJob> repository)
    {
        _logger = logger;
        _connectionPool = connectionPool;
        _blobStorageService = blobStorageService;
        _eventPublisher = eventPublisher;
        _repository = repository;

        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder().Handle<IOException>().Handle<TimeoutException>()
            })
            .Build();
    }

    /// <inheritdoc />
    public async Task<ProcessDiscoveredFileResult> Handle(ProcessDiscoveredFileCommand request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("ProcessDiscoveredFile");
        activity?.SetTag("file.name", request.FileName);
        activity?.SetTag("file.size", request.FileSize);
        activity?.SetTag("clearinghouse.code", request.ClearinghouseIdentifier.Code);
        activity?.SetTag("correlation.id", request.CorrelationId.Value);

        _logger.LogInformation(
            "Processing file {FileName} ({FileSize} bytes) from clearinghouse {ClearinghouseCode}",
            request.FileName,
            request.FileSize,
            request.ClearinghouseIdentifier.Code);

        ISftpClient? sftpClient = null;

        try
        {
            // Determine EDI transaction type from file name
            var ediType = DetermineEdiTransactionType(request.FileName);

            // Create ingested file entity
            var ingestedFile = IngestedFile.Create(
                request.IngestionJobId,
                request.FileName,
                request.FileSize,
                ediType,
                request.CorrelationId);

            // Acquire SFTP connection and download file as stream
            sftpClient = await _connectionPool.AcquireConnectionAsync(
                request.SftpConnectionDetails, cancellationToken);

            var remoteFilePath = $"{request.SftpConnectionDetails.DownloadDirectory}/{request.FileName}";

            // Download and process with retry
            var (contentHash, fileReference) = await _retryPipeline.ExecuteAsync(async ct =>
            {
                await using var downloadStream = await sftpClient.DownloadFileStreamAsync(remoteFilePath, ct);

                // Stream through MD5 hash computation while uploading to blob
                return await StreamDownloadAndUploadAsync(
                    downloadStream,
                    request.ClearinghouseIdentifier,
                    request.FileName,
                    request.FileSize,
                    request.CorrelationId,
                    ct);
            }, cancellationToken);

            // Update entity
            ingestedFile.SetContentHash(contentHash);
            ingestedFile.SetFileReference(fileReference);

            // Publish FileIngestedEvent
            var fileIngestedEvent = new FileIngestedEvent(
                fileReference,
                request.ClearinghouseIdentifier,
                ediType,
                request.CorrelationId);

            await _eventPublisher.PublishAsync(fileIngestedEvent, cancellationToken);

            activity?.SetTag("file.hash", contentHash);
            _logger.LogInformation(
                "Successfully processed file {FileName} with hash {ContentHash}",
                request.FileName,
                contentHash);

            return new ProcessDiscoveredFileResult(
                Success: true,
                FileReference: fileReference,
                ContentHash: contentHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process file {FileName} from clearinghouse {ClearinghouseCode}",
                request.FileName,
                request.ClearinghouseIdentifier.Code);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return new ProcessDiscoveredFileResult(
                Success: false,
                ErrorMessage: ex.Message);
        }
        finally
        {
            if (sftpClient is not null)
            {
                _connectionPool.ReleaseConnection(sftpClient);
            }
        }
    }

    private async Task<(string ContentHash, FileReference FileReference)> StreamDownloadAndUploadAsync(
        Stream downloadStream,
        ClearinghouseIdentifier clearinghouse,
        string fileName,
        long fileSize,
        CorrelationId correlationId,
        CancellationToken cancellationToken)
    {
        // Use a pipe stream pattern: compute hash while streaming to blob storage
        using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        using var hashStream = new HashComputingStream(downloadStream, md5);

        var containerName = $"edi-{clearinghouse.Code.ToLowerInvariant()}";
        var blobPath = $"{DateTime.UtcNow:yyyy/MM/dd}/{correlationId.Value}/{fileName}";

        var metadata = new Dictionary<string, string>
        {
            ["correlationId"] = correlationId.Value,
            ["clearinghouseCode"] = clearinghouse.Code,
            ["originalFileName"] = fileName,
            ["ingestedAt"] = DateTime.UtcNow.ToString("O")
        };

        var fileReference = await _blobStorageService.UploadAsync(
            containerName,
            blobPath,
            hashStream,
            contentType: "application/edi-x12",
            metadata: metadata,
            cancellationToken: cancellationToken);

        var contentHash = Convert.ToHexString(md5.GetHashAndReset()).ToLowerInvariant();

        return (contentHash, fileReference);
    }

    private static EdiTransactionType DetermineEdiTransactionType(string fileName)
    {
        var upperFileName = fileName.ToUpperInvariant();

        if (upperFileName.Contains("837")) return EdiTransactionType.Edi837;
        if (upperFileName.Contains("835")) return EdiTransactionType.Edi835;
        if (upperFileName.Contains("999")) return EdiTransactionType.Edi999;
        if (upperFileName.Contains("277")) return EdiTransactionType.Edi277;
        if (upperFileName.Contains("270")) return EdiTransactionType.Edi270;
        if (upperFileName.Contains("271")) return EdiTransactionType.Edi271;

        // Default to 837 for unrecognized patterns
        return EdiTransactionType.Edi837;
    }
}

/// <summary>
/// A stream wrapper that computes an incremental hash as data is read through it.
/// Ensures files are never fully loaded into memory.
/// </summary>
internal sealed class HashComputingStream : Stream
{
    private readonly Stream _innerStream;
    private readonly IncrementalHash _hash;

    public HashComputingStream(Stream innerStream, IncrementalHash hash)
    {
        _innerStream = innerStream;
        _hash = hash;
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _innerStream.Read(buffer, offset, count);
        if (bytesRead > 0)
        {
            _hash.AppendData(buffer, offset, bytesRead);
        }
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        if (bytesRead > 0)
        {
            _hash.AppendData(buffer, offset, bytesRead);
        }
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var bytesRead = await _innerStream.ReadAsync(buffer, cancellationToken);
        if (bytesRead > 0)
        {
            _hash.AppendData(buffer[..bytesRead].Span);
        }
        return bytesRead;
    }

    public override void Flush() => _innerStream.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _innerStream.Dispose();
        }
        base.Dispose(disposing);
    }
}
