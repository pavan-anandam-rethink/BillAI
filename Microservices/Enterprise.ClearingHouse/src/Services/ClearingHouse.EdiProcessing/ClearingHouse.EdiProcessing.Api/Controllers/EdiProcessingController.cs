using ClearingHouse.EdiProcessing.Application.Commands;
using ClearingHouse.EdiProcessing.Domain.Entities;
using ClearingHouse.EdiProcessing.Infrastructure.Persistence;
using ClearingHouse.SharedKernel;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClearingHouse.EdiProcessing.Api.Controllers;

/// <summary>
/// API controller for EDI file processing operations.
/// </summary>
[ApiController]
[Route("api/edi")]
public sealed class EdiProcessingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly EdiFileRepository _repository;
    private readonly ILogger<EdiProcessingController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EdiProcessingController"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator.</param>
    /// <param name="repository">The EDI file repository.</param>
    /// <param name="logger">The logger instance.</param>
    public EdiProcessingController(
        IMediator mediator,
        EdiFileRepository repository,
        ILogger<EdiProcessingController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Manually triggers processing of an EDI file.
    /// </summary>
    /// <param name="request">The processing request containing file reference and metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The processing result.</returns>
    [HttpPost("process")]
    [ProducesResponseType(typeof(ProcessingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessFile(
        [FromBody] ProcessEdiFileRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Manual EDI processing triggered for file. TransactionType: {TransactionType}, CorrelationId: {CorrelationId}",
            request.TransactionType,
            request.CorrelationId);

        var command = new ProcessEdiFileCommand(
            FileReference: request.FileReference,
            TransactionType: request.TransactionType,
            ClearinghouseType: request.ClearinghouseType,
            CorrelationId: request.CorrelationId);

        var result = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("EDI processing failed: {Error}", result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets the processing status of an EDI file.
    /// </summary>
    /// <param name="fileId">The EDI file identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file status information.</returns>
    [HttpGet("status/{fileId:guid}")]
    [ProducesResponseType(typeof(EdiFileStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid fileId, CancellationToken cancellationToken)
    {
        var file = await _repository.GetByIdAsync(fileId, cancellationToken).ConfigureAwait(false);

        if (file is null)
        {
            return NotFound(new { error = $"EDI file with ID '{fileId}' not found." });
        }

        var response = new EdiFileStatusResponse(
            FileId: file.Id,
            FileName: file.FileName,
            Status: file.Status.ToString(),
            CreatedAt: file.CreatedAt,
            CompletedAt: file.CompletedAt);

        return Ok(response);
    }

    /// <summary>
    /// Gets processing metrics for monitoring.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Aggregate processing metrics.</returns>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(ProcessingMetricsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken)
    {
        // Simple aggregate metrics - can be enhanced with more detailed queries
        await Task.CompletedTask.ConfigureAwait(false);

        var response = new ProcessingMetricsResponse(
            ServiceName: "ClearingHouse.EdiProcessing",
            Timestamp: DateTime.UtcNow);

        return Ok(response);
    }

    /// <summary>
    /// Retries failed segments for a specific EDI file.
    /// </summary>
    /// <param name="fileId">The EDI file identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The retry result.</returns>
    [HttpPost("retry/{fileId:guid}")]
    [ProducesResponseType(typeof(RetryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RetryFailedSegments(Guid fileId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retry requested for EDI file {FileId}", fileId);

        var file = await _repository.GetByIdAsync(fileId, cancellationToken).ConfigureAwait(false);
        if (file is null)
        {
            return NotFound(new { error = $"EDI file with ID '{fileId}' not found." });
        }

        var command = new RetryFailedSegmentsCommand(FileId: fileId);
        var result = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result);
    }
}

/// <summary>
/// Request model for manual EDI file processing.
/// </summary>
public sealed record ProcessEdiFileRequest(
    FileReference FileReference,
    EdiTransactionType TransactionType,
    ClearinghouseType ClearinghouseType,
    CorrelationId CorrelationId);

/// <summary>
/// Response model for EDI file status queries.
/// </summary>
public sealed record EdiFileStatusResponse(
    Guid FileId,
    string FileName,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt);

/// <summary>
/// Response model for processing metrics.
/// </summary>
public sealed record ProcessingMetricsResponse(
    string ServiceName,
    DateTime Timestamp);
