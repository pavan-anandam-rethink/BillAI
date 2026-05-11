using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reconciliation.Application.Commands.ReconcileClaim;
using Reconciliation.Application.Commands.ReconcilePayment;
using Reconciliation.Application.Queries.GetReconciliationStatus;

namespace Reconciliation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReconciliationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReconciliationController(IMediator mediator) => _mediator = mediator;

    [HttpPost("claim")]
    public async Task<IActionResult> ReconcileClaim([FromBody] ReconcileClaimCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("payment")]
    public async Task<IActionResult> ReconcilePayment([FromBody] ReconcilePaymentCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("status/{claimId}")]
    public async Task<IActionResult> GetStatus(string claimId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetReconciliationStatusQuery { ClaimId = claimId }, ct);
        return result is not null ? Ok(result) : NotFound();
    }
}
