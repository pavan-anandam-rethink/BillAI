using ClaimLifecycleMonitoring.Application.Configuration;
using ClaimLifecycleMonitoring.Application.Monitoring;
using Microsoft.Extensions.Options;

namespace ClaimLifecycleMonitoring.Worker;

/// <summary>
/// Long-running background service that periodically executes the
/// claim monitoring scan and rule engine.
/// </summary>
public sealed class ClaimMonitoringWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClaimMonitoringWorker> _logger;
    private readonly MonitoringOptions _options;

    public ClaimMonitoringWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<MonitoringOptions> options,
        ILogger<ClaimMonitoringWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, _options.ScanIntervalSeconds));
        _logger.LogInformation("ClaimMonitoringWorker started with scan interval {Interval}.", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var monitor = scope.ServiceProvider.GetRequiredService<IClaimMonitoringService>();
                var summary = await monitor.ScanAsync(stoppingToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "Monitoring scan completed. ClaimsExamined={Examined} AlertsRaised={Alerts} StuckClaims={Stuck}",
                    summary.ClaimsExamined, summary.AlertsRaised, summary.ClaimsMarkedStuck);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monitoring scan failed. Continuing on next cycle.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("ClaimMonitoringWorker stopping.");
    }
}
