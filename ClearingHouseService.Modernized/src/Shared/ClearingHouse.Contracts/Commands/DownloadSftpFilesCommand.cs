namespace ClearingHouse.Contracts.Commands;

public record DownloadSftpFilesCommand
{
    public int ClearinghouseId { get; init; }
    public string ClearinghouseName { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
}
