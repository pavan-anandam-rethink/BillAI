using ClearingHouse.SharedKernel.Models;
using MediatR;

namespace Notification.Application.Commands.CreateAlertRule;

public record CreateAlertRuleCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string? Condition { get; init; }
}
