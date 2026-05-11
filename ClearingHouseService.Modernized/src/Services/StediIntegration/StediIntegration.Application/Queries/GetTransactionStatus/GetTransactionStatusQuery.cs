using MediatR;

namespace StediIntegration.Application.Queries.GetTransactionStatus;

public record GetTransactionStatusQuery : IRequest<TransactionStatusDto?>
{
    public string StediTransactionId { get; init; } = string.Empty;
}

public record TransactionStatusDto
{
    public Guid Id { get; init; }
    public string StediTransactionId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
}
