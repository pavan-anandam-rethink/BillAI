using ClearingHouse.SftpIngestion.Domain.Interfaces;
using ClearingHouse.SftpIngestion.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace ClearingHouse.SftpIngestion.Infrastructure.Sftp;

/// <summary>
/// SFTP client implementation using the SSH.NET library.
/// Wraps synchronous SSH.NET operations with async patterns using Task.Run for I/O-bound operations.
/// </summary>
public sealed class SshNetSftpClient : Domain.Interfaces.ISftpClient
{
    private readonly ILogger<SshNetSftpClient> _logger;
    private SftpClient? _client;
    private SftpConnectionDetails? _connectionDetails;

    /// <summary>
    /// Initializes a new instance of the <see cref="SshNetSftpClient"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public SshNetSftpClient(ILogger<SshNetSftpClient> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsConnected => _client?.IsConnected ?? false;

    /// <inheritdoc />
    public async Task ConnectAsync(SftpConnectionDetails connectionDetails, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionDetails);

        _connectionDetails = connectionDetails;

        _logger.LogDebug(
            "Connecting to SFTP endpoint {Host}:{Port} as {Username}",
            connectionDetails.Host,
            connectionDetails.Port,
            connectionDetails.Username);

        var connectionInfo = new ConnectionInfo(
            connectionDetails.Host,
            connectionDetails.Port,
            connectionDetails.Username,
            new PasswordAuthenticationMethod(connectionDetails.Username, connectionDetails.EncryptedPassword))
        {
            Timeout = connectionDetails.ConnectionTimeout
        };

        _client = new SftpClient(connectionInfo);
        _client.OperationTimeout = connectionDetails.OperationTimeout;

        await Task.Run(() => _client.Connect(), cancellationToken);

        _logger.LogInformation(
            "Connected to SFTP endpoint {Host}:{Port}",
            connectionDetails.Host,
            connectionDetails.Port);
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_client is { IsConnected: true })
        {
            _logger.LogDebug("Disconnecting from SFTP endpoint {Host}", _connectionDetails?.Host);
            await Task.Run(() => _client.Disconnect(), cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<SftpFileEntry>> ListFilesAsync(
        string remoteDirectory,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteDirectory);

        _logger.LogDebug("Listing files in directory {RemoteDirectory}", remoteDirectory);

        var files = await Task.Run(() => _client!.ListDirectory(remoteDirectory), cancellationToken);

        var entries = files
            .Where(f => f.IsRegularFile)
            .Select(f => new SftpFileEntry(
                FileName: f.Name,
                FileSize: f.Length,
                LastModified: f.LastWriteTimeUtc))
            .ToList()
            .AsReadOnly();

        _logger.LogDebug("Found {FileCount} files in {RemoteDirectory}", entries.Count, remoteDirectory);
        return entries;
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadFileStreamAsync(
        string remoteFilePath,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteFilePath);

        _logger.LogDebug("Downloading file {RemoteFilePath}", remoteFilePath);

        // Open a read stream from the SFTP server — SSH.NET streams data on demand
        var stream = await Task.Run(() => _client!.OpenRead(remoteFilePath), cancellationToken);
        return stream;
    }

    /// <inheritdoc />
    public async Task DeleteFileAsync(string remoteFilePath, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteFilePath);

        _logger.LogDebug("Deleting file {RemoteFilePath}", remoteFilePath);
        await Task.Run(() => _client!.DeleteFile(remoteFilePath), cancellationToken);
        _logger.LogInformation("Deleted file {RemoteFilePath}", remoteFilePath);
    }

    /// <inheritdoc />
    public async Task UploadFileStreamAsync(
        Stream stream,
        string remoteFilePath,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteFilePath);

        _logger.LogDebug("Uploading file to {RemoteFilePath}", remoteFilePath);
        await Task.Run(() => _client!.UploadFile(stream, remoteFilePath), cancellationToken);
        _logger.LogInformation("Uploaded file to {RemoteFilePath}", remoteFilePath);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            if (_client.IsConnected)
            {
                await Task.Run(() => _client.Disconnect());
            }
            _client.Dispose();
            _client = null;
        }
    }

    private void EnsureConnected()
    {
        if (_client is not { IsConnected: true })
        {
            throw new InvalidOperationException("SFTP client is not connected. Call ConnectAsync first.");
        }
    }
}
