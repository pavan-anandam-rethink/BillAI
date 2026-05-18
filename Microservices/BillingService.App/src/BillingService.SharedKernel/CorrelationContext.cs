namespace BillingService.App.SharedKernel;

public interface ICorrelationContextAccessor
{
    string CorrelationId { get; set; }
}

public sealed class CorrelationContextAccessor : ICorrelationContextAccessor
{
    public string CorrelationId { get; set; } = string.Empty;
}

