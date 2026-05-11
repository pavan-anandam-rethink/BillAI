using Notification.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Queries.GetNotifications;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<GetNotificationsQueryHandler> _logger;

    public GetNotificationsQueryHandler(INotificationRepository repository, ILogger<GetNotificationsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Notification.Domain.Entities.NotificationRecord> records;

        if (request.UnsentOnly)
            records = await _repository.GetUnsentAsync(cancellationToken);
        else if (!string.IsNullOrEmpty(request.CorrelationId))
            records = await _repository.GetByCorrelationIdAsync(request.CorrelationId, cancellationToken);
        else
            records = await _repository.GetAllAsync(cancellationToken);

        return records.Select(r => new NotificationDto
        {
            Id = r.Id,
            Type = r.Type,
            Severity = r.Severity,
            Title = r.Title,
            Message = r.Message,
            CorrelationId = r.CorrelationId,
            IsSent = r.IsSent,
            IsRead = r.IsRead,
            CreatedAt = r.CreatedAt
        }).ToList();
    }
}
