using System.Collections.Concurrent;
using ClearingHouse.SftpIngestion.Domain.Interfaces;
using ClearingHouse.SftpIngestion.Domain.ValueObjects;
using ClearingHouse.SftpIngestion.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClearingHouse.SftpIngestion.Infrastructure.Sftp;

/// <summary>
/// Connection pool for SFTP clients using <see cref="ConcurrentDictionary{TKey,TValue}"/>-based pooling.
/// Provides semaphore-based concurrency control and automatic disposal of stale connections.
/// </summary>
public sealed class SftpConnectionPool : ISftpConnectionPool
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<ISftpClient>> _pool = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly ConcurrentDictionary<ISftpClient, string> _clientPoolKeys = new();
    private readonly ILogger<SftpConnectionPool> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly SftpIngestionOptions _options;
    private int _totalConnections;
    private int _activeConnections;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SftpConnectionPool"/> class.
    /// </summary>
    public SftpConnectionPool(
        ILogger<SftpConnectionPool> logger,
        ILoggerFactory loggerFactory,
        IOptions<SftpIngestionOptions> options)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<ISftpClient> AcquireConnectionAsync(
        SftpConnectionDetails connectionDetails,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(connectionDetails);

        var poolKey = connectionDetails.PoolKey;
        var semaphore = _semaphores.GetOrAdd(
            poolKey,
            _ => new SemaphoreSlim(_options.MaxConnectionsPerHost, _options.MaxConnectionsPerHost));

        _logger.LogDebug("Acquiring SFTP connection for {PoolKey}", poolKey);

        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var bag = _pool.GetOrAdd(poolKey, _ => new ConcurrentBag<ISftpClient>());

            // Try to reuse an existing healthy connection
            while (bag.TryTake(out var existingClient))
            {
                if (existingClient.IsConnected)
                {
                    Interlocked.Increment(ref _activeConnections);
                    _clientPoolKeys[existingClient] = poolKey;
                    _logger.LogDebug("Reusing pooled SFTP connection for {PoolKey}", poolKey);
                    return existingClient;
                }

                // Dispose stale connection
                _logger.LogDebug("Disposing stale SFTP connection for {PoolKey}", poolKey);
                _clientPoolKeys.TryRemove(existingClient, out _);
                await existingClient.DisposeAsync();
                Interlocked.Decrement(ref _totalConnections);
            }

            // Create new connection
            var client = new SshNetSftpClient(_loggerFactory.CreateLogger<SshNetSftpClient>());
            await client.ConnectAsync(connectionDetails, cancellationToken);
            Interlocked.Increment(ref _totalConnections);
            Interlocked.Increment(ref _activeConnections);
            _clientPoolKeys[client] = poolKey;

            _logger.LogInformation("Created new SFTP connection for {PoolKey}. Total: {TotalConnections}", poolKey, _totalConnections);
            return client;
        }
        catch
        {
            semaphore.Release();
            throw;
        }
    }

    /// <inheritdoc />
    public void ReleaseConnection(ISftpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        Interlocked.Decrement(ref _activeConnections);

        if (!_clientPoolKeys.TryRemove(client, out var poolKey) || _disposed || !client.IsConnected)
        {
            _ = DisposeClientAsync(client);
            if (poolKey is not null && _semaphores.TryGetValue(poolKey, out var sem))
            {
                sem.Release();
            }
            return;
        }

        // Return to correct pool using tracked key
        var pool = _pool.GetOrAdd(poolKey, _ => new ConcurrentBag<ISftpClient>());
        pool.Add(client);
        _logger.LogDebug("Released SFTP connection back to pool {PoolKey}", poolKey);

        if (_semaphores.TryGetValue(poolKey, out var semaphore))
        {
            semaphore.Release();
        }
    }

    /// <inheritdoc />
    public PoolStatus GetPoolStatus()
    {
        return new PoolStatus(
            TotalConnections: _totalConnections,
            ActiveConnections: _activeConnections,
            AvailableConnections: _totalConnections - _activeConnections);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogInformation("Disposing SFTP connection pool");

        foreach (var (_, bag) in _pool)
        {
            while (bag.TryTake(out var client))
            {
                await client.DisposeAsync();
            }
        }

        _pool.Clear();

        foreach (var (_, semaphore) in _semaphores)
        {
            semaphore.Dispose();
        }

        _semaphores.Clear();
    }

    private async Task DisposeClientAsync(ISftpClient client)
    {
        try
        {
            await client.DisposeAsync();
            Interlocked.Decrement(ref _totalConnections);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing SFTP client");
        }
    }
}
