using ClaimLifecycleMonitoring.Application.Abstractions.Notifications;
using ClaimLifecycleMonitoring.Application.Abstractions.Persistence;
using ClaimLifecycleMonitoring.Application.Abstractions.RuleEngine;
using ClaimLifecycleMonitoring.Application.Abstractions.Time;
using ClaimLifecycleMonitoring.Application.Common.Mapping;
using ClaimLifecycleMonitoring.Application.Configuration;
using ClaimLifecycleMonitoring.Domain.Entities;
using ClaimLifecycleMonitoring.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClaimLifecycleMonitoring.Application.Monitoring;

/// <summary>
/// Result summary from a single monitoring scan iteration.
/// </summary>
public sealed class ClaimMonitoringScanResult
{
    /// <summary>Number of claims examined during the scan.</summary>
    public int ClaimsExamined { get; set; }
    /// <summary>Number of alerts raised during the scan.</summary>
    public int AlertsRaised { get; set; }
    /// <summary>Number of claims transitioned to a stuck state.</summary>
    public int ClaimsMarkedStuck { get; set; }
}

/// <summary>
/// Encapsulates the algorithm used to detect stuck / timed out / SLA breached claims
/// and raise corresponding alerts. Reused by both the background worker and the API's
/// on-demand monitoring endpoint.
/// </summary>
public interface IClaimMonitoringService
{
    /// <summary>Executes a monitoring scan across all claims eligible for evaluation.</summary>
    Task<ClaimMonitoringScanResult> ScanAsync(CancellationToken cancellationToken);
}

/// <inheritdoc cref="IClaimMonitoringService"/>
public sealed class ClaimMonitoringService : IClaimMonitoringService
{
    private readonly IClaimRepository _claimRepository;
    private readonly IStageSlaConfigurationRepository _slaRepository;
    private readonly IRuleEngine _ruleEngine;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly IClaimNotificationPublisher _publisher;
    private readonly MonitoringOptions _options;
    private readonly ILogger<ClaimMonitoringService> _logger;

    /// <summary>Creates a new <see cref="ClaimMonitoringService"/>.</summary>
    public ClaimMonitoringService(
        IClaimRepository claimRepository,
        IStageSlaConfigurationRepository slaRepository,
        IRuleEngine ruleEngine,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        IClaimNotificationPublisher publisher,
        IOptions<MonitoringOptions> options,
        ILogger<ClaimMonitoringService> logger)
    {
        _claimRepository = claimRepository;
        _slaRepository = slaRepository;
        _ruleEngine = ruleEngine;
        _uow = uow;
        _clock = clock;
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ClaimMonitoringScanResult> ScanAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var result = new ClaimMonitoringScanResult();

        var slaList = await _slaRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var slaMap = slaList.ToDictionary(s => s.Stage, s => s);

        // Fetch a bounded window of active claims to evaluate. Persistence is expected to
        // provide a query that only returns non-terminal claims.
        var oldest = now.AddHours(-_options.GlobalClaimTimeoutHours * 2);
        var candidates = await _claimRepository
            .GetPotentiallyStuckAsync(oldest, _options.BatchSize, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Monitoring scan starting for {Count} candidate claims at {Now:O}", candidates.Count, now);

        var anyChanges = false;

        foreach (var claim in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.ClaimsExamined++;

            var evaluations = _ruleEngine.Evaluate(claim, now, slaMap);
            if (evaluations.Count == 0)
            {
                continue;
            }

            foreach (var evaluation in evaluations)
            {
                if (evaluation.MarkStuck && claim.CurrentStatus != ClaimStatus.Stuck)
                {
                    claim.MarkStuck(evaluation.Category, evaluation.Message, now);
                    result.ClaimsMarkedStuck++;
                }

                var alert = ClaimAlert.Create(evaluation.Category, evaluation.Severity, evaluation.Message, claim.CurrentStage, now);
                claim.RaiseAlert(alert);
                result.AlertsRaised++;
                anyChanges = true;

                _logger.LogInformation("Alert raised for claim {ClaimNumber}: {RuleId} [{Severity}] {Message}",
                    claim.ClaimNumber, evaluation.RuleId, evaluation.Severity, evaluation.Message);
            }
        }

        if (anyChanges)
        {
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Publish notifications AFTER persistence so listeners see committed state.
            foreach (var claim in candidates.Where(c => c.Alerts.Any(a => a.RaisedUtc == now)))
            {
                var dto = claim.ToDto(_clock);
                await _publisher.PublishClaimUpdatedAsync(dto, cancellationToken).ConfigureAwait(false);
                foreach (var alert in claim.Alerts.Where(a => a.RaisedUtc == now))
                {
                    await _publisher.PublishAlertRaisedAsync(alert.ToDto(claim.ClaimNumber), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        _logger.LogInformation("Monitoring scan complete. Examined={Examined} Alerts={Alerts} Stuck={Stuck}",
            result.ClaimsExamined, result.AlertsRaised, result.ClaimsMarkedStuck);
        return result;
    }
}
