namespace BillingService.Contracts;

public sealed record BillingIntegrationEvent
{
    public required Guid EventId { get; init; }
    public required string EventType { get; init; }
    public required DateTimeOffset OccurredAtUtc { get; init; }
    public required string CorrelationId { get; init; }
    public required int AccountInfoId { get; init; }
    public required string PayloadJson { get; init; }
    public int SchemaVersion { get; init; } = 1;
}
