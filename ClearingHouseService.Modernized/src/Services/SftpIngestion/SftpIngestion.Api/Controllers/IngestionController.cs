using MediatR;
using Microsoft.AspNetCore.Mvc;
using SftpIngestion.Application.Commands.IngestFiles;
using SftpIngestion.Application.Commands.UploadFile;
using SftpIngestion.Application.Queries.GetIngestionStatus;

namespace SftpIngestion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<IngestionController> _logger;

    public IngestionController(IMediator mediator, ILogger<IngestionController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> IngestFiles([FromBody] IngestFilesCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromBody] UploadFileCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("status/{fileId:guid}")]
    public async Task<IActionResult> GetStatus(Guid fileId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetIngestionStatusQuery { FileId = fileId }, cancellationToken);
        return result is not null ? Ok(result) : NotFound();
    }
}
