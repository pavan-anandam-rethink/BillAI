using MediatR;

namespace IdentityService.Domain.Events;

public record TokenRefreshedEvent(
    string UserId,
    string AccountInfoId,
    DateTime Timestamp) : INotification;
