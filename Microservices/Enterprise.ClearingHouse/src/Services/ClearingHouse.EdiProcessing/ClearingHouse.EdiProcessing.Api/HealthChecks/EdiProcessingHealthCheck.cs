using ClearingHouse.EdiProcessing.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearingHouse.EdiProcessing.Api.HealthChecks;

/// <summary>
/// Health check implementation that verifies EDI processing service dependencies.
/// </summary>
public sealed class EdiProcessingHealthCheck : IHealthCheck
{
    private readonly EdiProcessingDbContext _dbContext;
    private readonly ILogger<EdiProcessingHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EdiProcessingHealthCheck"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public EdiProcessingHealthCheck(
        EdiProcessingDbContext dbContext,
        ILogger<EdiProcessingHealthCheck> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database
                .CanConnectAsync(cancellationToken)
                .ConfigureAwait(false);

            if (canConnect)
            {
                return HealthCheckResult.Healthy("EDI Processing database is accessible.");
            }

            _logger.LogWarning("EDI Processing database connectivity check failed");
            return HealthCheckResult.Degraded("EDI Processing database is not accessible.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EDI Processing health check failed with exception");
            return HealthCheckResult.Unhealthy(
                "EDI Processing service is unhealthy.",
                exception: ex);
        }
    }
}
