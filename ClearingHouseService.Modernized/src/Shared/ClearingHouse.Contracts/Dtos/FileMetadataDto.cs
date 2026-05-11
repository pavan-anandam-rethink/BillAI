namespace ClearingHouse.Contracts.Dtos;

public record FileMetadataDto
{
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string BlobUri { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string ContentHash { get; init; } = string.Empty;
    public string ClearinghouseName { get; init; } = string.Empty;
    public int ClearinghouseId { get; init; }
    public int EdiTransactionType { get; init; }
    public string Status { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public int RetryCount { get; init; }
    public string? ErrorMessage { get; init; }
}
