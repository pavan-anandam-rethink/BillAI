namespace BillingService.Application.Abstractions.Correlation;

/// <summary>
/// Provides the correlation ID for the current request or operation.
/// The correlation ID is propagated from the inbound X-Correlation-Id HTTP header
/// and injected into outbound log entries, integration events, and service calls.
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Returns the correlation ID for the current ambient context, or null when called
    /// outside of an active HTTP request (e.g. from a background worker).
    /// </summary>
    string? GetCorrelationId();
}
