using ClearingHouse.SharedKernel.Interfaces;
using Notification.Domain.Entities;

namespace Notification.Domain.Interfaces;

public interface INotificationRepository : IRepository<NotificationRecord>
{
    Task<IReadOnlyList<NotificationRecord>> GetUnsentAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationRecord>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
}
