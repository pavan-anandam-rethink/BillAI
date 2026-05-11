using MediatR;
using Microsoft.AspNetCore.Mvc;
using BlobManagement.Application.Commands.UploadBlob;
using BlobManagement.Application.Commands.MoveBlob;
using BlobManagement.Application.Commands.ArchiveBlob;
using BlobManagement.Application.Queries.GetBlobMetadata;
using BlobManagement.Application.Queries.ListBlobs;

namespace BlobManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlobController : ControllerBase
{
    private readonly IMediator _mediator;

    public BlobController(IMediator mediator) => _mediator = mediator;

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromBody] UploadBlobCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("move")]
    public async Task<IActionResult> Move([FromBody] MoveBlobCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("archive")]
    public async Task<IActionResult> Archive([FromBody] ArchiveBlobCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMetadata(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBlobMetadataQuery { BlobFileId = id }, ct);
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpGet("container/{containerName}")]
    public async Task<IActionResult> ListBlobs(string containerName, CancellationToken ct)
    {
        var result = await _mediator.Send(new ListBlobsQuery { ContainerName = containerName }, ct);
        return Ok(result);
    }
}
