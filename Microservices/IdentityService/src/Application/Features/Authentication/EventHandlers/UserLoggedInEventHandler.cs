using IdentityService.Domain.Entities;
using IdentityService.Domain.Events;
using IdentityService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Features.Authentication.EventHandlers;

public class UserLoggedInEventHandler(
    IAuditLogRepository auditLogRepository,
    ILogger<UserLoggedInEventHandler> logger) : INotificationHandler<UserLoggedInEvent>
{
    public async Task Handle(UserLoggedInEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {UserId} logged in from {IpAddress}", notification.UserId, notification.IpAddress);

        var auditLog = new AuditLog
        {
            UserId = notification.UserId,
            Action = "Login",
            EntityName = "User",
            EntityId = notification.UserId,
            Timestamp = notification.Timestamp,
            IpAddress = notification.IpAddress
        };

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
    }
}
