using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using SftpIngestion.Domain.Interfaces;

namespace SftpIngestion.Infrastructure.Sftp;

public sealed class SftpClientWrapper : ISftpClientWrapper
{
    private readonly SftpClient _client;
    private readonly ILogger<SftpClientWrapper> _logger;
    private bool _disposed;

    public bool IsConnected => _client.IsConnected;

    public SftpClientWrapper(string host, int port, string username, string password, ILogger<SftpClientWrapper> logger)
    {
        _logger = logger;
        var connectionInfo = new ConnectionInfo(host, port, username,
            new PasswordAuthenticationMethod(username, password))
        {
            Timeout = TimeSpan.FromSeconds(30),
            RetryAttempts = 3
        };
        _client = new SftpClient(connectionInfo);
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            _client.Connect();
            _logger.LogInformation("SFTP connection established to {Host}", _client.ConnectionInfo.Host);
        }, cancellationToken);
    }

    public Task<IReadOnlyList<SftpFileInfo>> ListFilesAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<SftpFileInfo>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var files = _client.ListDirectory(directoryPath)
                .Where(f => !f.IsDirectory && f.Name != "." && f.Name != "..")
                .Select(f => new SftpFileInfo(f.Name, f.FullName, f.Length, f.LastWriteTimeUtc))
                .ToList();
            return files;
        }, cancellationToken);
    }

    public Task<Stream> DownloadFileStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.Run<Stream>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var memoryStream = new MemoryStream();
            _client.DownloadFile(filePath, memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }, cancellationToken);
    }

    public Task UploadFileAsync(Stream content, string remotePath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            _client.UploadFile(content, remotePath);
        }, cancellationToken);
    }

    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            _client.DeleteFile(filePath);
        }, cancellationToken);
    }

    public Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!_client.IsConnected) _client.Connect();
                return _client.IsConnected;
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (_client.IsConnected) _client.Disconnect();
            _client.Dispose();
        }
        return ValueTask.CompletedTask;
    }
}
