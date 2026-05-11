using ClearingHouse.SharedKernel.Models;
using StediIntegration.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace StediIntegration.Application.Commands.SubmitTransaction;

public class SubmitTransactionCommandHandler : IRequestHandler<SubmitTransactionCommand, Result<string>>
{
    private readonly IStediApiClient _stediClient;
    private readonly IStediTransactionRepository _repository;
    private readonly ILogger<SubmitTransactionCommandHandler> _logger;

    public SubmitTransactionCommandHandler(IStediApiClient stediClient, IStediTransactionRepository repository, ILogger<SubmitTransactionCommandHandler> logger)
    {
        _stediClient = stediClient;
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(SubmitTransactionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Submitting transaction for file {FileId}, CorrelationId: {CorrelationId}", request.FileId, request.CorrelationId);
        await Task.CompletedTask;
        return Result.Success("transaction-pending");
    }
}
