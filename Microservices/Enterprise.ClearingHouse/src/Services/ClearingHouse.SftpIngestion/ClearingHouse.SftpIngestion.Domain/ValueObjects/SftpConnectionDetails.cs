namespace ClearingHouse.SftpIngestion.Domain.ValueObjects;

/// <summary>
/// Value object containing SFTP connection configuration details.
/// </summary>
/// <param name="Host">The SFTP server hostname or IP address.</param>
/// <param name="Port">The SFTP server port (default 22).</param>
/// <param name="Username">The authentication username.</param>
/// <param name="EncryptedPassword">The encrypted password for authentication.</param>
/// <param name="UploadDirectory">The remote directory for uploading files.</param>
/// <param name="DownloadDirectory">The remote directory for downloading files.</param>
/// <param name="ConnectionTimeout">Connection timeout duration.</param>
/// <param name="OperationTimeout">Individual operation timeout duration.</param>
public sealed record SftpConnectionDetails(
    string Host,
    int Port,
    string Username,
    string EncryptedPassword,
    string UploadDirectory,
    string DownloadDirectory,
    TimeSpan ConnectionTimeout,
    TimeSpan OperationTimeout)
{
    /// <summary>
    /// Creates a new instance with default timeout values.
    /// </summary>
    /// <param name="host">The SFTP server hostname.</param>
    /// <param name="port">The SFTP server port.</param>
    /// <param name="username">The authentication username.</param>
    /// <param name="encryptedPassword">The encrypted password.</param>
    /// <param name="downloadDirectory">The remote download directory.</param>
    /// <param name="uploadDirectory">The remote upload directory.</param>
    /// <returns>A new <see cref="SftpConnectionDetails"/> instance.</returns>
    public static SftpConnectionDetails Create(
        string host,
        int port,
        string username,
        string encryptedPassword,
        string downloadDirectory,
        string uploadDirectory = "/outbound")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        return new SftpConnectionDetails(
            Host: host,
            Port: port,
            Username: username,
            EncryptedPassword: encryptedPassword,
            UploadDirectory: uploadDirectory,
            DownloadDirectory: downloadDirectory,
            ConnectionTimeout: TimeSpan.FromSeconds(30),
            OperationTimeout: TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Gets the connection string key for pooling purposes.
    /// </summary>
    public string PoolKey => $"{Host}:{Port}:{Username}";
}
