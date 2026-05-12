using ClearingHouse.SharedKernel.Domain;
using EdiProcessing.Domain.Entities;
using MediatR;

namespace EdiProcessing.Application.Commands;

public record ProcessEdiFileCommand(
    Guid FileId,
    string FileName,
    string BlobUri,
    EdiFileType FileType,
    string ClearinghouseId,
    string CorrelationId,
    string? BatchId = null) : IRequest<Result>;
