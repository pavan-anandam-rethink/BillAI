using MediatR;
using Microsoft.AspNetCore.Mvc;
using BatchOrchestration.Application.Commands.CreateBatch;
using BatchOrchestration.Application.Commands.CompleteBatchItem;
using BatchOrchestration.Application.Queries.GetBatchStatus;

namespace BatchOrchestration.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BatchController : ControllerBase
{
    private readonly IMediator _mediator;

    public BatchController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBatchCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("complete-item")]
    public async Task<IActionResult> CompleteItem([FromBody] CompleteBatchItemCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{batchId:guid}")]
    public async Task<IActionResult> GetStatus(Guid batchId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBatchStatusQuery { BatchId = batchId }, ct);
        return result is not null ? Ok(result) : NotFound();
    }
}
