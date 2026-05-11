using MediatR;

namespace Notification.Application.Queries.GetNotifications;

public record GetNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>
{
    public string? CorrelationId { get; init; }
    public bool UnsentOnly { get; init; }
}

public record NotificationDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public bool IsSent { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}
