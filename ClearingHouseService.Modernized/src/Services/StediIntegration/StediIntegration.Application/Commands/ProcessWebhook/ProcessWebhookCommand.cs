using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace StediIntegration.Application.Commands.ProcessWebhook;

public record ProcessWebhookCommand : IRequest<Result>
{
    public string WebhookId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
}
