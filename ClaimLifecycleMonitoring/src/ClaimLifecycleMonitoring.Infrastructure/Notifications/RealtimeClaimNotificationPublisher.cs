using ClaimLifecycleMonitoring.Application.Abstractions.Notifications;
using ClaimLifecycleMonitoring.Contracts.Alerts;
using ClaimLifecycleMonitoring.Contracts.Claims;
using Microsoft.Extensions.Logging;

namespace ClaimLifecycleMonitoring.Infrastructure.Notifications;

/// <summary>
/// Abstraction over the concrete real-time transport (SignalR in the API host,
/// no-op in the worker host, etc.). Kept as a plain contract to keep the
/// Application layer free from any transport-specific dependencies.
/// </summary>
public interface IRealtimeClaimBroadcaster
{
    /// <summary>Broadcasts a claim update to all connected clients.</summary>
    Task BroadcastClaimUpdatedAsync(ClaimDto claim, CancellationToken cancellationToken);

    /// <summary>Broadcasts a newly raised alert to all connected clients.</summary>
    Task BroadcastAlertRaisedAsync(ClaimAlertDto alert, CancellationToken cancellationToken);
}

/// <summary>
/// Default publisher that fans out claim update notifications to an
/// <see cref="IRealtimeClaimBroadcaster"/>. Any transport failure is logged
/// and swallowed so business flow is unaffected.
/// </summary>
public sealed class RealtimeClaimNotificationPublisher : IClaimNotificationPublisher
{
    private readonly IRealtimeClaimBroadcaster _broadcaster;
    private readonly ILogger<RealtimeClaimNotificationPublisher> _logger;

    /// <summary>Creates a new <see cref="RealtimeClaimNotificationPublisher"/>.</summary>
    public RealtimeClaimNotificationPublisher(IRealtimeClaimBroadcaster broadcaster, ILogger<RealtimeClaimNotificationPublisher> logger)
    {
        _broadcaster = broadcaster;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PublishClaimUpdatedAsync(ClaimDto claim, CancellationToken cancellationToken)
    {
        try
        {
            await _broadcaster.BroadcastClaimUpdatedAsync(claim, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast claim update for {ClaimNumber}", claim.ClaimNumber);
        }
    }

    /// <inheritdoc />
    public async Task PublishAlertRaisedAsync(ClaimAlertDto alert, CancellationToken cancellationToken)
    {
        try
        {
            await _broadcaster.BroadcastAlertRaisedAsync(alert, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast alert for claim {ClaimNumber}", alert.ClaimNumber);
        }
    }
}

/// <summary>
/// No-op broadcaster used when the host does not expose a real-time endpoint
/// (e.g. the background worker). Prevents null reference exceptions when the
/// publisher is invoked outside of the API process.
/// </summary>
public sealed class NullRealtimeClaimBroadcaster : IRealtimeClaimBroadcaster
{
    /// <inheritdoc />
    public Task BroadcastClaimUpdatedAsync(ClaimDto claim, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task BroadcastAlertRaisedAsync(ClaimAlertDto alert, CancellationToken cancellationToken) => Task.CompletedTask;
}
