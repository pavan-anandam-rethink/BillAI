namespace ClearingHouse.SharedKernel.Observability;

public interface ICorrelationContext
{
    string CorrelationId { get; }
    string? CausationId { get; }
    string? UserId { get; }
    string? TenantId { get; }
    IDictionary<string, string> Properties { get; }
}

public class CorrelationContext : ICorrelationContext
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string? CausationId { get; set; }
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
}
