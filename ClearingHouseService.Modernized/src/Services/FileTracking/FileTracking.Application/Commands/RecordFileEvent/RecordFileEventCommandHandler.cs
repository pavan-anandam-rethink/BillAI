using ClearingHouse.SharedKernel.Enums;
using ClearingHouse.SharedKernel.Models;
using FileTracking.Domain.Entities;
using FileTracking.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FileTracking.Application.Commands.RecordFileEvent;

public class RecordFileEventCommandHandler : IRequestHandler<RecordFileEventCommand, Result>
{
    private readonly IFileTrackingRepository _repository;
    private readonly ILogger<RecordFileEventCommandHandler> _logger;

    public RecordFileEventCommandHandler(IFileTrackingRepository repository, ILogger<RecordFileEventCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(RecordFileEventCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Recording file event for {FileId}: {Status}", request.FileId, request.Status);

        var existing = await _repository.GetByFileIdAsync(request.FileId, cancellationToken);
        if (existing is null)
        {
            var record = FileTrackingRecord.Create(request.FileId, request.FileName, request.ClearinghouseId, request.ClearinghouseName, request.CorrelationId);
            await _repository.AddAsync(record, cancellationToken);
        }
        else
        {
            if (Enum.TryParse<FileProcessingStatus>(request.Status, out var status))
                existing.UpdateStatus(status, request.Description);
            await _repository.UpdateAsync(existing, cancellationToken);
        }

        return Result.Success();
    }
}
