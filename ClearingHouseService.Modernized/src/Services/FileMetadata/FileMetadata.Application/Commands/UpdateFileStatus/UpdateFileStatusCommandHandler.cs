using ClearingHouse.SharedKernel.Models;
using FileMetadata.Domain.Entities;
using FileMetadata.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FileMetadata.Application.Commands.UpdateFileStatus;

public class UpdateFileStatusCommandHandler : IRequestHandler<UpdateFileStatusCommand, Result>
{
    private readonly IFileMetadataRepository _repository;
    private readonly IFileEventHistoryRepository _eventHistoryRepository;
    private readonly ILogger<UpdateFileStatusCommandHandler> _logger;

    public UpdateFileStatusCommandHandler(IFileMetadataRepository repository, IFileEventHistoryRepository eventHistoryRepository, ILogger<UpdateFileStatusCommandHandler> logger)
    {
        _repository = repository;
        _eventHistoryRepository = eventHistoryRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateFileStatusCommand request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.FileId, cancellationToken);
        if (record is null) return Result.Failure("File not found");

        if (request.Status == "Processing") record.MarkAsProcessing();
        else if (request.Status == "Processed") record.MarkAsProcessed();
        else if (request.Status == "Failed") record.MarkAsFailed(request.ErrorMessage ?? "Unknown error");

        await _repository.UpdateAsync(record, cancellationToken);

        var eventHistory = FileEventHistory.Create(request.FileId, request.Status, $"Status updated to {request.Status}", request.CorrelationId, request.ErrorMessage);
        await _eventHistoryRepository.AddAsync(eventHistory, cancellationToken);

        return Result.Success();
    }
}
