using StediIntegration.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace StediIntegration.Application.Queries.GetTransactionStatus;

public class GetTransactionStatusQueryHandler : IRequestHandler<GetTransactionStatusQuery, TransactionStatusDto?>
{
    private readonly IStediTransactionRepository _repository;
    private readonly ILogger<GetTransactionStatusQueryHandler> _logger;

    public GetTransactionStatusQueryHandler(IStediTransactionRepository repository, ILogger<GetTransactionStatusQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<TransactionStatusDto?> Handle(GetTransactionStatusQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _repository.GetByStediTransactionIdAsync(request.StediTransactionId, cancellationToken);
        if (transaction is null) return null;

        return new TransactionStatusDto
        {
            Id = transaction.Id,
            StediTransactionId = transaction.StediTransactionId,
            Status = transaction.Status,
            Direction = transaction.Direction,
            CorrelationId = transaction.CorrelationId,
            CompletedAt = transaction.CompletedAt,
            ErrorMessage = transaction.ErrorMessage
        };
    }
}
