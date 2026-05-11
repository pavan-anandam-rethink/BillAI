using MediatR;
using Microsoft.AspNetCore.Mvc;
using FileTracking.Application.Commands.RecordFileEvent;
using FileTracking.Application.Queries.GetFileTimeline;
using FileTracking.Application.Queries.SearchFileTracking;

namespace FileTracking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileTrackingController : ControllerBase
{
    private readonly IMediator _mediator;

    public FileTrackingController(IMediator mediator) => _mediator = mediator;

    [HttpPost("event")]
    public async Task<IActionResult> RecordEvent([FromBody] RecordFileEventCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("timeline/{fileId:guid}")]
    public async Task<IActionResult> GetTimeline(Guid fileId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFileTimelineQuery { FileId = fileId }, ct);
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? correlationId, [FromQuery] int? clearinghouseId, CancellationToken ct)
    {
        var result = await _mediator.Send(new SearchFileTrackingQuery { CorrelationId = correlationId, ClearinghouseId = clearinghouseId }, ct);
        return Ok(result);
    }
}
