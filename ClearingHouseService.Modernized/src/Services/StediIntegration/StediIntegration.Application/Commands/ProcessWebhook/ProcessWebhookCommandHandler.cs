using ClearingHouse.SharedKernel.Models;
using StediIntegration.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace StediIntegration.Application.Commands.ProcessWebhook;

public class ProcessWebhookCommandHandler : IRequestHandler<ProcessWebhookCommand, Result>
{
    private readonly ILogger<ProcessWebhookCommandHandler> _logger;

    public ProcessWebhookCommandHandler(ILogger<ProcessWebhookCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<Result> Handle(ProcessWebhookCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing Stedi webhook {WebhookId}, EventType: {EventType}", request.WebhookId, request.EventType);
        await Task.CompletedTask;
        return Result.Success();
    }
}
