namespace SftpIngestion.Domain.Interfaces;

public interface ISftpClientFactory
{
    ISftpClientWrapper CreateClient(string host, int port, string userName, string password);
}

public interface ISftpClientWrapper : IDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    bool IsConnected { get; }
    Task<IReadOnlyList<SftpFileInfo>> ListDirectoryAsync(string path, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);
    Task UploadAsync(Stream stream, string path, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
}

public record SftpFileInfo(string Name, string FullName, long Length, DateTime LastModified, bool IsDirectory);
