namespace SftpIngestion.Domain.Interfaces;

public interface ISftpClientWrapper : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SftpFileInfo>> ListFilesAsync(string directoryPath, CancellationToken cancellationToken = default);
    Task<Stream> DownloadFileStreamAsync(string filePath, CancellationToken cancellationToken = default);
    Task UploadFileAsync(Stream content, string remotePath, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }
}

public record SftpFileInfo(string Name, string FullPath, long Size, DateTime LastModified);
