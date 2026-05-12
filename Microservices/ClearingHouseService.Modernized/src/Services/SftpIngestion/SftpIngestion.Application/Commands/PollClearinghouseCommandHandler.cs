using ClearingHouse.SharedKernel.Domain;
using ClearingHouse.SharedKernel.Messaging;
using ClearingHouse.SharedKernel.Storage;
using MediatR;
using Microsoft.Extensions.Logging;
using SftpIngestion.Domain.Entities;
using SftpIngestion.Domain.Interfaces;

namespace SftpIngestion.Application.Commands;

public class PollClearinghouseCommandHandler : IRequestHandler<PollClearinghouseCommand, Result>
{
    private readonly ISftpConnectionPool _connectionPool;
    private readonly IInboundFileRepository _fileRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PollClearinghouseCommandHandler> _logger;

    public PollClearinghouseCommandHandler(
        ISftpConnectionPool connectionPool,
        IInboundFileRepository fileRepository,
        IBlobStorageService blobStorage,
        IEventPublisher eventPublisher,
        ILogger<PollClearinghouseCommandHandler> logger)
    {
        _connectionPool = connectionPool;
        _fileRepository = fileRepository;
        _blobStorage = blobStorage;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(PollClearinghouseCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Polling clearinghouse {ClearinghouseId} with correlation {CorrelationId}",
            request.ClearinghouseId, request.CorrelationId);

        ISftpClientWrapper? client = null;
        try
        {
            client = await _connectionPool.GetConnectionAsync(request.ClearinghouseId, cancellationToken);
            var files = await client.ListFilesAsync("/inbound", cancellationToken);

            _logger.LogInformation("Found {FileCount} files on {ClearinghouseId}", files.Count, request.ClearinghouseId);

            foreach (var fileInfo in files)
            {
                await ProcessFileAsync(client, fileInfo, request.ClearinghouseId, request.CorrelationId, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling clearinghouse {ClearinghouseId}", request.ClearinghouseId);
            if (client != null)
                await _connectionPool.InvalidateConnectionAsync(request.ClearinghouseId, cancellationToken);
            return Result.Failure($"Polling failed: {ex.Message}");
        }
        finally
        {
            if (client != null)
                await _connectionPool.ReleaseConnectionAsync(request.ClearinghouseId, client, cancellationToken);
        }
    }

    private async Task ProcessFileAsync(
        ISftpClientWrapper client, SftpFileInfo fileInfo,
        string clearinghouseId, string correlationId, CancellationToken cancellationToken)
    {
        var inboundFile = InboundFile.Create(
            fileInfo.Name, fileInfo.FullPath, clearinghouseId, fileInfo.Size, correlationId);

        try
        {
            await _fileRepository.AddAsync(inboundFile, cancellationToken);

            await using var stream = await client.DownloadFileStreamAsync(fileInfo.FullPath, cancellationToken);

            var containerName = $"inbound-{clearinghouseId.ToLowerInvariant()}";
            var blobName = $"{DateTime.UtcNow:yyyy/MM/dd}/{inboundFile.Id}/{fileInfo.Name}";

            var metadata = new Dictionary<string, string>
            {
                ["correlationId"] = correlationId,
                ["clearinghouseId"] = clearinghouseId,
                ["originalFileName"] = fileInfo.Name,
                ["fileId"] = inboundFile.Id.ToString(),
                ["detectedAt"] = DateTime.UtcNow.ToString("O")
            };

            var blobUri = await _blobStorage.UploadAsync(stream, containerName, blobName, metadata, cancellationToken);

            inboundFile.MarkUploadedToBlob(blobUri);
            await _fileRepository.UpdateAsync(inboundFile, cancellationToken);

            await _eventPublisher.PublishAsync(new FileIngestedIntegrationEvent
            {
                CorrelationId = correlationId,
                FileId = inboundFile.Id,
                FileName = fileInfo.Name,
                ClearinghouseId = clearinghouseId,
                BlobUri = blobUri,
                FileSizeBytes = fileInfo.Size
            }, "file-ingested-topic", cancellationToken);

            _logger.LogInformation("File {FileName} ingested successfully. BlobUri: {BlobUri}", fileInfo.Name, blobUri);
        }
        catch (Exception ex)
        {
            inboundFile.MarkFailed(ex.Message);
            await _fileRepository.UpdateAsync(inboundFile, cancellationToken);
            _logger.LogError(ex, "Failed to process file {FileName} from {ClearinghouseId}", fileInfo.Name, clearinghouseId);
        }
    }
}

public record FileIngestedIntegrationEvent : ClearingHouse.SharedKernel.Messaging.IntegrationEvent
{
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ClearinghouseId { get; init; } = string.Empty;
    public string BlobUri { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
}
