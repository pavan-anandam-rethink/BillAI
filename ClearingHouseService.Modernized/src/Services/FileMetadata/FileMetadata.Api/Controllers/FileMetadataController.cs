using MediatR;
using Microsoft.AspNetCore.Mvc;
using FileMetadata.Application.Commands.CreateFileMetadata;
using FileMetadata.Application.Commands.UpdateFileStatus;
using FileMetadata.Application.Queries.GetFileMetadata;
using FileMetadata.Application.Queries.SearchFiles;

namespace FileMetadata.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileMetadataController : ControllerBase
{
    private readonly IMediator _mediator;

    public FileMetadataController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFileMetadataCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateFileStatusCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{fileId:guid}")]
    public async Task<IActionResult> Get(Guid fileId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFileMetadataQuery { FileId = fileId }, ct);
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] int? clearinghouseId, [FromQuery] string? correlationId, CancellationToken ct)
    {
        var result = await _mediator.Send(new SearchFilesQuery { ClearinghouseId = clearinghouseId, CorrelationId = correlationId }, ct);
        return Ok(result);
    }
}
