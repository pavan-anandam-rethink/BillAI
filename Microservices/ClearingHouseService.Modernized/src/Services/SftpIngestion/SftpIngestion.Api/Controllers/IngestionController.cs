using ClearingHouse.SharedKernel.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SftpIngestion.Application.Commands;

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

    [HttpPost("poll/{clearinghouseId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PollClearinghouse(string clearinghouseId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        var result = await _mediator.Send(new PollClearinghouseCommand(clearinghouseId, correlationId), cancellationToken);

        return result.IsSuccess ? Ok(new { Message = "Polling completed", CorrelationId = correlationId })
            : BadRequest(new { Error = result.Error, CorrelationId = correlationId });
    }

    [HttpPost("poll/all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PollAllClearinghouses(CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString();
        var clearinghouses = new[] { "Stedi", "Availity", "TriZetto", "Sandata", "Waystar" };
        var results = new Dictionary<string, string>();

        var tasks = clearinghouses.Select(async ch =>
        {
            var result = await _mediator.Send(new PollClearinghouseCommand(ch, correlationId), cancellationToken);
            return (ch, result);
        });

        foreach (var (ch, result) in await Task.WhenAll(tasks))
        {
            results[ch] = result.IsSuccess ? "Success" : result.Error;
        }

        return Ok(new { CorrelationId = correlationId, Results = results });
    }
}
