using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace Notification.Application.Commands.SendNotification;

public record SendNotificationCommand : IRequest<Result>
{
    public string Type { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}
