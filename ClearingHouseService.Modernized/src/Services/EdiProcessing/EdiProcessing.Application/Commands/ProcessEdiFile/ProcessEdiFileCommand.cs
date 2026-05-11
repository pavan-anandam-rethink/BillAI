using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace EdiProcessing.Application.Commands.ProcessEdiFile;

public record ProcessEdiFileCommand : IRequest<Result<Guid>>
{
    public Guid FileId { get; init; }
    public string BlobUri { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int EdiTransactionType { get; init; }
    public int ClearinghouseId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public Guid BatchId { get; init; }
}
