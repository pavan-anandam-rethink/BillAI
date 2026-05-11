namespace ClearingHouse.Contracts.Commands;

public record SubmitClaimCommand
{
    public Guid ClaimId { get; init; }
    public int ClearinghouseId { get; init; }
    public string AccountId { get; init; } = string.Empty;
    public bool IsSecondary { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
}
