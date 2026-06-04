namespace BillingService.Application.Abstractions.Correlation;

/// <summary>
/// Provides the current request's correlation ID for distributed tracing.
/// In web contexts this is populated by CorrelationIdMiddleware from the X-Correlation-Id header.
/// In background worker contexts implementations may source the ID from message headers.
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Returns the correlation ID for the current operation, or null when one is not available.
    /// </summary>
    string? GetCorrelationId();
}
