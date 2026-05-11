namespace ClearingHouse.Contracts.Events;

public record ClaimReconciliationEvent
{
    public Guid ReconciliationId { get; init; }
    public string ClaimId { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public DateTime ReconciledAt { get; init; } = DateTime.UtcNow;
}
