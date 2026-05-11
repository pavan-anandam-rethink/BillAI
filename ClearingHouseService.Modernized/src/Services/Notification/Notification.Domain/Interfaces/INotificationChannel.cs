using ClearingHouse.SharedKernel.Models;
using Notification.Domain.Entities;

namespace Notification.Domain.Interfaces;

public interface INotificationChannel
{
    string ChannelName { get; }
    Task<Result> SendAsync(NotificationRecord notification, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
