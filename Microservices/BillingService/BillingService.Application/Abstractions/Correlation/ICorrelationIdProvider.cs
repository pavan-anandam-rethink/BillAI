namespace BillingService.Application.Abstractions.Correlation;

/// <summary>
/// Provides the current request's correlation identifier.
/// Implementations resolve the ID from the ambient request context (e.g. HttpContext).
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>Gets the correlation ID for the current request, or null when unavailable.</summary>
    string? GetCorrelationId();
}
