namespace BillingService.Application.Abstractions.Correlation;

/// <summary>
/// Provides the correlation ID for the current request or operation scope.
/// The correlation ID is set once per HTTP request by CorrelationIdMiddleware and
/// flows through the entire call chain — including domain services, infrastructure adapters,
/// and outbox messages — enabling end-to-end distributed tracing.
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Returns the correlation ID for the current scope.
    /// Returns an empty string when no correlation ID has been established.
    /// </summary>
    string CorrelationId { get; }
}
