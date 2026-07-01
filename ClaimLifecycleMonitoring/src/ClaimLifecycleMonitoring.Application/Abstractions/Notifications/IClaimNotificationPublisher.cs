using ClaimLifecycleMonitoring.Contracts.Alerts;
using ClaimLifecycleMonitoring.Contracts.Claims;

namespace ClaimLifecycleMonitoring.Application.Abstractions.Notifications;

/// <summary>
/// Publishes claim lifecycle updates to interested downstream consumers
/// (dashboards, message brokers, etc.). Implementations are non-throwing:
/// failures must be logged and swallowed to avoid blocking business flow.
/// </summary>
public interface IClaimNotificationPublisher
{
    /// <summary>Publishes a claim update.</summary>
    Task PublishClaimUpdatedAsync(ClaimDto claim, CancellationToken cancellationToken);

    /// <summary>Publishes a newly raised alert.</summary>
    Task PublishAlertRaisedAsync(ClaimAlertDto alert, CancellationToken cancellationToken);
}
