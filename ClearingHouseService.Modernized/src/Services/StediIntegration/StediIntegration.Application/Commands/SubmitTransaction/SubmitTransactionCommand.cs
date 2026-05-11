using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace StediIntegration.Application.Commands.SubmitTransaction;

public record SubmitTransactionCommand : IRequest<Result<string>>
{
    public Guid FileId { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public string BlobUri { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}
