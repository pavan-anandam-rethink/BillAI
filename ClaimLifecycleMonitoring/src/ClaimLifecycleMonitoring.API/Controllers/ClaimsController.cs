using ClaimLifecycleMonitoring.Application.Features.Claims.Commands;
using ClaimLifecycleMonitoring.Application.Features.Claims.Queries;
using ClaimLifecycleMonitoring.Contracts.Claims;
using ClaimLifecycleMonitoring.Contracts.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaimLifecycleMonitoring.API.Controllers;

/// <summary>Endpoints exposing the claim lifecycle monitoring domain.</summary>
[ApiController]
[Route("api/v1/claims")]
[Produces("application/json")]
public sealed class ClaimsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Creates a new <see cref="ClaimsController"/>.</summary>
    public ClaimsController(IMediator mediator) { _mediator = mediator; }

    /// <summary>Registers a new claim under monitoring.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClaimDto>> Create([FromBody] CreateClaimRequest request, CancellationToken cancellationToken)
    {
        var claim = await _mediator.Send(new CreateClaimCommand(request), cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = claim.Id }, claim);
    }

    /// <summary>Searches for claims matching the supplied filter.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClaimDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClaimDto>>> Search([FromQuery] ClaimSearchQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SearchClaimsQuery(query ?? new ClaimSearchQuery()), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Returns a single claim by its numeric identifier.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimDto>> GetById([FromRoute] long id, CancellationToken cancellationToken)
    {
        var claim = await _mediator.Send(new GetClaimByIdQuery(id), cancellationToken).ConfigureAwait(false);
        return Ok(claim);
    }

    /// <summary>Returns a single claim by its claim number.</summary>
    [HttpGet("by-number/{claimNumber}")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimDto>> GetByNumber([FromRoute] string claimNumber, CancellationToken cancellationToken)
    {
        var claim = await _mediator.Send(new GetClaimByNumberQuery(claimNumber), cancellationToken).ConfigureAwait(false);
        return Ok(claim);
    }

    /// <summary>Returns the full lifecycle history for a claim.</summary>
    [HttpGet("{id:long}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<ClaimHistoryEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClaimHistoryEntryDto>>> GetHistory([FromRoute] long id, CancellationToken cancellationToken)
    {
        var history = await _mediator.Send(new GetClaimHistoryQuery(id), cancellationToken).ConfigureAwait(false);
        return Ok(history);
    }

    /// <summary>Advances a claim to the specified stage.</summary>
    [HttpPost("{id:long}/advance")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClaimDto>> Advance([FromRoute] long id, [FromBody] AdvanceClaimStageRequest request, CancellationToken cancellationToken)
    {
        var claim = await _mediator.Send(new AdvanceClaimStageCommand(id, request), cancellationToken).ConfigureAwait(false);
        return Ok(claim);
    }

    /// <summary>Records a failure against a claim.</summary>
    [HttpPost("{id:long}/failure")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClaimDto>> RecordFailure([FromRoute] long id, [FromBody] RecordFailureRequest request, CancellationToken cancellationToken)
    {
        var claim = await _mediator.Send(new RecordFailureCommand(id, request), cancellationToken).ConfigureAwait(false);
        return Ok(claim);
    }

    /// <summary>Retries a failed claim.</summary>
    [HttpPost("{id:long}/retry")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClaimDto>> Retry([FromRoute] long id, [FromBody] RetryClaimRequest request, CancellationToken cancellationToken)
    {
        var claim = await _mediator.Send(new RetryClaimCommand(id, request ?? new RetryClaimRequest()), cancellationToken).ConfigureAwait(false);
        return Ok(claim);
    }

    /// <summary>Cancels a claim.</summary>
    [HttpPost("{id:long}/cancel")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClaimDto>> Cancel([FromRoute] long id, [FromBody] CancelClaimRequest request, CancellationToken cancellationToken)
    {
        var claim = await _mediator.Send(new CancelClaimCommand(id, request), cancellationToken).ConfigureAwait(false);
        return Ok(claim);
    }

    /// <summary>Applies a payment to a claim.</summary>
    [HttpPost("{id:long}/payments")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClaimDto>> ApplyPayment([FromRoute] long id, [FromBody] ApplyPaymentRequest request, CancellationToken cancellationToken)
    {
        var claim = await _mediator.Send(new ApplyPaymentCommand(id, request), cancellationToken).ConfigureAwait(false);
        return Ok(claim);
    }

    /// <summary>Marks a claim as a duplicate of another.</summary>
    [HttpPost("{id:long}/duplicate/{duplicateOfClaimNumber}")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClaimDto>> MarkDuplicate([FromRoute] long id, [FromRoute] string duplicateOfClaimNumber, CancellationToken cancellationToken)
    {
        var claim = await _mediator.Send(new MarkDuplicateCommand(id, duplicateOfClaimNumber), cancellationToken).ConfigureAwait(false);
        return Ok(claim);
    }
}
