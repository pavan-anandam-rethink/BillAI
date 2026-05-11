namespace ClearingHouse.Contracts.Dtos;

public record BatchStatusDto
{
    public Guid BatchId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalFiles { get; init; }
    public int ProcessedFiles { get; init; }
    public int FailedFiles { get; init; }
    public int PendingFiles { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public double ProgressPercentage => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
}
