using ClaimLifecycleMonitoring.Application.Features.Alerts;
using ClaimLifecycleMonitoring.Application.Features.Dashboard.Queries;
using ClaimLifecycleMonitoring.Application.Monitoring;
using ClaimLifecycleMonitoring.Contracts.Alerts;
using ClaimLifecycleMonitoring.Contracts.Claims;
using ClaimLifecycleMonitoring.Contracts.Common;
using ClaimLifecycleMonitoring.Contracts.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaimLifecycleMonitoring.API.Controllers;

/// <summary>Endpoints returning the operations dashboard snapshot.</summary>
[ApiController]
[Route("api/v1/dashboard")]
[Produces("application/json")]
public sealed class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Creates a new <see cref="DashboardController"/>.</summary>
    public DashboardController(IMediator mediator) { _mediator = mediator; }

    /// <summary>Returns the current dashboard snapshot.</summary>
    [HttpGet("snapshot")]
    [ProducesResponseType(typeof(DashboardSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSnapshotDto>> Snapshot(CancellationToken cancellationToken)
    {
        var snapshot = await _mediator.Send(new GetDashboardSnapshotQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(snapshot);
    }
}

/// <summary>Endpoints exposing monitoring alerts.</summary>
[ApiController]
[Route("api/v1/alerts")]
[Produces("application/json")]
public sealed class AlertsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Creates a new <see cref="AlertsController"/>.</summary>
    public AlertsController(IMediator mediator) { _mediator = mediator; }

    /// <summary>Searches monitoring alerts.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClaimAlertDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClaimAlertDto>>> Search(
        [FromQuery] bool? acknowledged,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new SearchAlertsQuery(acknowledged, page, pageSize), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Acknowledges an alert.</summary>
    [HttpPost("{id:long}/acknowledge")]
    [ProducesResponseType(typeof(ClaimAlertDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClaimAlertDto>> Acknowledge([FromRoute] long id, [FromBody] AcknowledgeAlertRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AcknowledgeAlertCommand(id, request), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }
}

/// <summary>Endpoints allowing operators to trigger the monitoring engine on-demand.</summary>
[ApiController]
[Route("api/v1/monitoring")]
[Produces("application/json")]
public sealed class MonitoringController : ControllerBase
{
    private readonly IClaimMonitoringService _service;

    /// <summary>Creates a new <see cref="MonitoringController"/>.</summary>
    public MonitoringController(IClaimMonitoringService service) { _service = service; }

    /// <summary>Runs a monitoring scan immediately and returns the result.</summary>
    [HttpPost("scan")]
    [ProducesResponseType(typeof(ClaimMonitoringScanResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClaimMonitoringScanResult>> Scan(CancellationToken cancellationToken)
    {
        var result = await _service.ScanAsync(cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }
}
