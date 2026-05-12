using ClearingHouse.SftpIngestion.Domain.ValueObjects;

namespace ClearingHouse.SftpIngestion.Domain.Interfaces;

/// <summary>
/// Represents a file entry discovered on the SFTP endpoint.
/// </summary>
/// <param name="FileName">The file name.</param>
/// <param name="FileSize">The file size in bytes.</param>
/// <param name="LastModified">The last modified timestamp.</param>
public sealed record SftpFileEntry(string FileName, long FileSize, DateTime LastModified);

/// <summary>
/// Interface for SFTP client operations against a clearinghouse endpoint.
/// </summary>
public interface ISftpClient : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the client is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the SFTP endpoint using the provided connection details.
    /// </summary>
    /// <param name="connectionDetails">The SFTP connection configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ConnectAsync(SftpConnectionDetails connectionDetails, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the SFTP endpoint.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files available in the specified remote directory.
    /// </summary>
    /// <param name="remoteDirectory">The remote directory to list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of file entries found in the directory.</returns>
    Task<IReadOnlyCollection<SftpFileEntry>> ListFilesAsync(
        string remoteDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from the SFTP endpoint as a stream.
    /// </summary>
    /// <param name="remoteFilePath">The full remote path of the file to download.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream containing the file content.</returns>
    Task<Stream> DownloadFileStreamAsync(
        string remoteFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the SFTP endpoint.
    /// </summary>
    /// <param name="remoteFilePath">The full remote path of the file to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteFileAsync(string remoteFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file stream to the SFTP endpoint.
    /// </summary>
    /// <param name="stream">The content stream to upload.</param>
    /// <param name="remoteFilePath">The target remote file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UploadFileStreamAsync(
        Stream stream,
        string remoteFilePath,
        CancellationToken cancellationToken = default);
}
