using MediatR;

namespace IdentityService.Domain.Events;

public record UserLoggedInEvent(
    string UserId,
    string AccountInfoId,
    string IpAddress,
    DateTime Timestamp) : INotification;
