using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notification.Application.Commands.SendNotification;
using Notification.Application.Commands.CreateAlertRule;
using Notification.Application.Queries.GetNotifications;

namespace Notification.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendNotificationCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("alert-rule")]
    public async Task<IActionResult> CreateAlertRule([FromBody] CreateAlertRuleCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] string? correlationId, [FromQuery] bool unsentOnly, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetNotificationsQuery { CorrelationId = correlationId, UnsentOnly = unsentOnly }, ct);
        return Ok(result);
    }
}
