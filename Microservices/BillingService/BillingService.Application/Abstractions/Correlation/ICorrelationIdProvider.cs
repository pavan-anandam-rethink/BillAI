namespace BillingService.Application.Abstractions.Correlation;

public interface ICorrelationIdProvider
{
    string? GetCorrelationId();
}
