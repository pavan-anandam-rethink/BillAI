namespace BillingService.Application.Abstractions.Correlation;

/// <summary>
/// Provides the correlation ID for the current operation.
/// Resolved from the incoming HTTP request header (X-Correlation-Id) by the
/// web-layer implementation, or generated fresh for background workers.
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Returns the correlation ID for the current scope.
    /// Returns null when invoked outside a request or worker context that has not
    /// been seeded with a correlation ID.
    /// </summary>
    string? GetCorrelationId();
}
