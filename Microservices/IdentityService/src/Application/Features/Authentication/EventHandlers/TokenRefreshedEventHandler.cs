using IdentityService.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Features.Authentication.EventHandlers;

public class TokenRefreshedEventHandler(ILogger<TokenRefreshedEventHandler> logger)
    : INotificationHandler<TokenRefreshedEvent>
{
    public Task Handle(TokenRefreshedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Token refreshed for user {UserId}, account {AccountId}",
            notification.UserId, notification.AccountInfoId);
        return Task.CompletedTask;
    }
}
