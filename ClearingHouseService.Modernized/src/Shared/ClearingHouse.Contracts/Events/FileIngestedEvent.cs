namespace ClearingHouse.Contracts.Events;

public record FileIngestedEvent
{
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string BlobUri { get; init; } = string.Empty;
    public string ClearinghouseName { get; init; } = string.Empty;
    public int ClearinghouseId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string ContentHash { get; init; } = string.Empty;
    public DateTime IngestedAt { get; init; } = DateTime.UtcNow;
    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
