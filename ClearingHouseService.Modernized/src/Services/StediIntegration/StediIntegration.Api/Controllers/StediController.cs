using MediatR;
using Microsoft.AspNetCore.Mvc;
using StediIntegration.Application.Commands.SubmitTransaction;
using StediIntegration.Application.Commands.ProcessWebhook;
using StediIntegration.Application.Queries.GetTransactionStatus;

namespace StediIntegration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StediController : ControllerBase
{
    private readonly IMediator _mediator;

    public StediController(IMediator mediator) => _mediator = mediator;

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitTransactionCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] ProcessWebhookCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("status/{transactionId}")]
    public async Task<IActionResult> GetStatus(string transactionId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTransactionStatusQuery { StediTransactionId = transactionId }, ct);
        return result is not null ? Ok(result) : NotFound();
    }
}
