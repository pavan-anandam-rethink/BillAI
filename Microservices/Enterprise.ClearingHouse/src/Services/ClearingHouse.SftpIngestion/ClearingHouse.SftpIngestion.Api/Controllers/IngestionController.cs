using ClearingHouse.SharedKernel.Domain.Interfaces;
using ClearingHouse.SharedKernel.Domain.ValueObjects;
using ClearingHouse.SftpIngestion.Application.Commands;
using ClearingHouse.SftpIngestion.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClearingHouse.SftpIngestion.Api.Controllers;

/// <summary>
/// API controller for managing SFTP ingestion operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class IngestionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRepository<SftpIngestionJob> _repository;
    private readonly ILogger<IngestionController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionController"/> class.
    /// </summary>
    public IngestionController(
        IMediator mediator,
        IRepository<SftpIngestionJob> repository,
        ILogger<IngestionController> logger)
    {
        _mediator = mediator;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Starts an ingestion job for the specified clearinghouse.
    /// </summary>
    /// <param name="clearinghouse">The clearinghouse code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created ingestion job details.</returns>
    [HttpPost("start/{clearinghouse}")]
    [ProducesResponseType(typeof(StartIngestionResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartIngestion(
        [FromRoute] string clearinghouse,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received ingestion request for clearinghouse {Clearinghouse}", clearinghouse);

        var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString("D");

        var command = new StartIngestionCommand(
            ClearinghouseIdentifier: new ClearinghouseIdentifier(clearinghouse, clearinghouse, ConnectionType.Sftp),
            CorrelationId: CorrelationId.From(correlationId),
            ForceExecution: false);

        var result = await _mediator.Send(command, cancellationToken);
        return Accepted(result);
    }

    /// <summary>
    /// Gets the status of a specific ingestion job.
    /// </summary>
    /// <param name="jobId">The ingestion job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ingestion job details.</returns>
    [HttpGet("status/{jobId:guid}")]
    [ProducesResponseType(typeof(IngestionJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(
        [FromRoute] Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return NotFound(new { Message = $"Ingestion job {jobId} not found." });
        }

        return Ok(MapToDto(job));
    }

    /// <summary>
    /// Gets the history of ingestion jobs with pagination.
    /// </summary>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of ingestion jobs.</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<IngestionJobDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var jobs = await _repository.Query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(jobs.Select(MapToDto));
    }

    /// <summary>
    /// Forces an immediate poll of the specified clearinghouse.
    /// </summary>
    /// <param name="request">The force poll request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created ingestion job details.</returns>
    [HttpPost("force-poll")]
    [ProducesResponseType(typeof(StartIngestionResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForcePoll(
        [FromBody] ForcePollRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received force-poll request for clearinghouse {Clearinghouse}", request.ClearinghouseCode);

        var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString("D");

        var command = new StartIngestionCommand(
            ClearinghouseIdentifier: new ClearinghouseIdentifier(request.ClearinghouseCode, request.ClearinghouseCode, ConnectionType.Sftp),
            CorrelationId: CorrelationId.From(correlationId),
            ForceExecution: true);

        var result = await _mediator.Send(command, cancellationToken);
        return Accepted(result);
    }

    private static IngestionJobDto MapToDto(SftpIngestionJob job) => new(
        Id: job.Id,
        ClearinghouseCode: job.ClearinghouseIdentifier.Code,
        ClearinghouseName: job.ClearinghouseIdentifier.Name,
        Status: job.Status.ToString(),
        FilesDiscovered: job.FilesDiscovered,
        FilesProcessed: job.FilesProcessed,
        ErrorMessage: job.ErrorMessage,
        LastPolledAt: job.LastPolledAt,
        CreatedAt: job.CreatedAt);
}

/// <summary>
/// DTO for ingestion job responses.
/// </summary>
public sealed record IngestionJobDto(
    Guid Id,
    string ClearinghouseCode,
    string ClearinghouseName,
    string Status,
    int FilesDiscovered,
    int FilesProcessed,
    string? ErrorMessage,
    DateTime? LastPolledAt,
    DateTime CreatedAt);

/// <summary>
/// Request model for force-polling a clearinghouse.
/// </summary>
public sealed record ForcePollRequest(string ClearinghouseCode);
