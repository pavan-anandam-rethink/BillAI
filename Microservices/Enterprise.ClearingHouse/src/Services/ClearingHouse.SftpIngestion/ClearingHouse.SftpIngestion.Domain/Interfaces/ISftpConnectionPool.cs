using ClearingHouse.SftpIngestion.Domain.ValueObjects;

namespace ClearingHouse.SftpIngestion.Domain.Interfaces;

/// <summary>
/// Represents the current status of the SFTP connection pool.
/// </summary>
/// <param name="TotalConnections">Total number of connections in the pool.</param>
/// <param name="ActiveConnections">Number of currently active connections.</param>
/// <param name="AvailableConnections">Number of available idle connections.</param>
public sealed record PoolStatus(int TotalConnections, int ActiveConnections, int AvailableConnections);

/// <summary>
/// Interface for managing a pool of SFTP connections with concurrency control.
/// </summary>
public interface ISftpConnectionPool : IAsyncDisposable
{
    /// <summary>
    /// Acquires an SFTP client connection from the pool, creating one if necessary.
    /// </summary>
    /// <param name="connectionDetails">The SFTP connection configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A connected SFTP client instance.</returns>
    Task<ISftpClient> AcquireConnectionAsync(
        SftpConnectionDetails connectionDetails,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases an SFTP client connection back to the pool.
    /// </summary>
    /// <param name="client">The SFTP client to release.</param>
    void ReleaseConnection(ISftpClient client);

    /// <summary>
    /// Gets the current status of the connection pool.
    /// </summary>
    /// <returns>The pool status information.</returns>
    PoolStatus GetPoolStatus();
}
