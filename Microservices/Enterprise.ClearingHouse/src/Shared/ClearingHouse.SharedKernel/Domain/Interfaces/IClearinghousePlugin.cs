using ClearingHouse.SharedKernel.Domain.ValueObjects;

namespace ClearingHouse.SharedKernel.Domain.Interfaces;

/// <summary>
/// Plugin interface for clearinghouse-specific integrations.
/// Each clearinghouse adapter implements this interface.
/// </summary>
public interface IClearinghousePlugin
{
    /// <summary>
    /// Gets the identifier for this clearinghouse.
    /// </summary>
    ClearinghouseIdentifier Identifier { get; }

    /// <summary>
    /// Uploads a file to the clearinghouse.
    /// </summary>
    /// <param name="stream">The file content stream.</param>
    /// <param name="fileName">The target file name.</param>
    /// <param name="transactionType">The EDI transaction type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UploadFileAsync(
        Stream stream,
        string fileName,
        EdiTransactionType transactionType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads available files from the clearinghouse.
    /// </summary>
    /// <param name="transactionType">Optional filter by EDI transaction type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of downloaded file streams with metadata.</returns>
    Task<IReadOnlyCollection<DownloadedFile>> DownloadFilesAsync(
        EdiTransactionType? transactionType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the connection to the clearinghouse.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the connection is valid; otherwise false.</returns>
    Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current health status of the clearinghouse connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The health status information.</returns>
    Task<ClearinghouseHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a file downloaded from a clearinghouse.
/// </summary>
/// <param name="FileName">The name of the downloaded file.</param>
/// <param name="Content">The file content stream.</param>
/// <param name="TransactionType">The EDI transaction type.</param>
/// <param name="DownloadedAt">The UTC timestamp when the file was downloaded.</param>
public sealed record DownloadedFile(
    string FileName,
    Stream Content,
    EdiTransactionType TransactionType,
    DateTime DownloadedAt);

/// <summary>
/// Represents the health status of a clearinghouse connection.
/// </summary>
/// <param name="IsHealthy">Whether the connection is healthy.</param>
/// <param name="Message">A descriptive status message.</param>
/// <param name="LastCheckedAt">The UTC timestamp of the last health check.</param>
public sealed record ClearinghouseHealthStatus(
    bool IsHealthy,
    string Message,
    DateTime LastCheckedAt);
