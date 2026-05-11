using Microsoft.Extensions.Logging;
using Renci.SshNet;
using SftpIngestion.Domain.Interfaces;

namespace SftpIngestion.Infrastructure.Sftp;

public class SshNetSftpClientWrapper : ISftpClientWrapper
{
    private readonly SftpClient _client;
    private readonly ILogger<SshNetSftpClientWrapper> _logger;

    public SshNetSftpClientWrapper(string host, int port, string userName, string password, ILogger<SshNetSftpClientWrapper> logger)
    {
        _client = new SftpClient(host, port, userName, password);
        _client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(180);
        _logger = logger;
    }

    public bool IsConnected => _client.IsConnected;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _client.Connect();
        _logger.LogInformation("Connected to SFTP server {Host}", _client.ConnectionInfo.Host);
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        if (_client.IsConnected) _client.Disconnect();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SftpFileInfo>> ListDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        var files = _client.ListDirectory(path)
            .Where(f => !f.IsDirectory && f.Name != "." && f.Name != "..")
            .Select(f => new SftpFileInfo(f.Name, f.FullName, f.Length, f.LastWriteTime, f.IsDirectory))
            .ToList();
        return Task.FromResult<IReadOnlyList<SftpFileInfo>>(files);
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var stream = _client.OpenRead(path);
        return Task.FromResult<Stream>(stream);
    }

    public Task UploadAsync(Stream stream, string path, CancellationToken cancellationToken = default)
    {
        _client.UploadFile(stream, path);
        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        _client.DeleteFile(path);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
