using ClearingHouse.SharedKernel.Models;
using Notification.Domain.Entities;
using Notification.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Commands.SendNotification;

public class SendNotificationCommandHandler : IRequestHandler<SendNotificationCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<SendNotificationCommandHandler> _logger;

    public SendNotificationCommandHandler(INotificationRepository repository, ILogger<SendNotificationCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending notification: {Title}", request.Title);

        var notification = NotificationRecord.Create(request.Type, request.Severity, request.Title, request.Message, request.CorrelationId);
        await _repository.AddAsync(notification, cancellationToken);
        return Result.Success();
    }
}
