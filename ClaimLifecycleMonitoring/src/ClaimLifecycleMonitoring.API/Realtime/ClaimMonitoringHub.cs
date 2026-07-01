using ClaimLifecycleMonitoring.Contracts.Alerts;
using ClaimLifecycleMonitoring.Contracts.Claims;
using ClaimLifecycleMonitoring.Infrastructure.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace ClaimLifecycleMonitoring.API.Realtime;

/// <summary>
/// SignalR hub used to push claim updates and monitoring alerts to connected clients
/// (typically the operations dashboard).
/// </summary>
public sealed class ClaimMonitoringHub : Hub
{
    /// <summary>The hub path clients must connect to.</summary>
    public const string Path = "/hubs/claim-monitoring";

    /// <summary>Client method name used for claim updates.</summary>
    public const string ClaimUpdatedMethod = "claimUpdated";

    /// <summary>Client method name used for alert notifications.</summary>
    public const string AlertRaisedMethod = "alertRaised";
}

/// <summary>SignalR-backed implementation of <see cref="IRealtimeClaimBroadcaster"/>.</summary>
public sealed class SignalRClaimBroadcaster : IRealtimeClaimBroadcaster
{
    private readonly IHubContext<ClaimMonitoringHub> _hub;

    /// <summary>Creates a new <see cref="SignalRClaimBroadcaster"/>.</summary>
    public SignalRClaimBroadcaster(IHubContext<ClaimMonitoringHub> hub) { _hub = hub; }

    /// <inheritdoc />
    public Task BroadcastClaimUpdatedAsync(ClaimDto claim, CancellationToken cancellationToken)
        => _hub.Clients.All.SendAsync(ClaimMonitoringHub.ClaimUpdatedMethod, claim, cancellationToken);

    /// <inheritdoc />
    public Task BroadcastAlertRaisedAsync(ClaimAlertDto alert, CancellationToken cancellationToken)
        => _hub.Clients.All.SendAsync(ClaimMonitoringHub.AlertRaisedMethod, alert, cancellationToken);
}
