namespace BillingService.Application.Abstractions.Correlation;

/// <summary>
/// Provides read access to the correlation ID resolved for the current ambient execution
/// context (HTTP request, background job, or Service Bus message handler).
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Returns the correlation ID for the current unit of work, or a newly generated
    /// fallback value when no ID is available in the ambient context.
    /// </summary>
    string GetCorrelationId();
}
