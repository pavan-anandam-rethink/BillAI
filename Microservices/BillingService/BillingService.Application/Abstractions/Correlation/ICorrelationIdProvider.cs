namespace BillingService.Application.Abstractions.Correlation;

/// <summary>
/// Provides the correlation ID for the current request or operation.
/// The correlation ID is propagated via the X-Correlation-Id HTTP header.
/// </summary>
public interface ICorrelationIdProvider
{
    string? GetCorrelationId();
}
