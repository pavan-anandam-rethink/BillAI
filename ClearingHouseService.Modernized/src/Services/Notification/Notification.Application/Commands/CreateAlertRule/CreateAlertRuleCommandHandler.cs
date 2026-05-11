using ClearingHouse.SharedKernel.Models;
using Notification.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Commands.CreateAlertRule;

public class CreateAlertRuleCommandHandler : IRequestHandler<CreateAlertRuleCommand, Result<Guid>>
{
    private readonly ILogger<CreateAlertRuleCommandHandler> _logger;

    public CreateAlertRuleCommandHandler(ILogger<CreateAlertRuleCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateAlertRuleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating alert rule: {Name}", request.Name);
        var rule = AlertRule.Create(request.Name, request.EventType, request.Severity, request.Channel, request.Condition);
        await Task.CompletedTask;
        return Result.Success(rule.Id);
    }
}
