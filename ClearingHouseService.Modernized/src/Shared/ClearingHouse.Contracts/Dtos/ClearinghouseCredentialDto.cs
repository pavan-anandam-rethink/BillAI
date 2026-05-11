namespace ClearingHouse.Contracts.Dtos;

public record ClearinghouseCredentialDto
{
    public int ClearinghouseId { get; init; }
    public string ClearinghouseName { get; init; } = string.Empty;
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string UploadDirectory { get; init; } = string.Empty;
    public string DownloadDirectory { get; init; } = string.Empty;
}
