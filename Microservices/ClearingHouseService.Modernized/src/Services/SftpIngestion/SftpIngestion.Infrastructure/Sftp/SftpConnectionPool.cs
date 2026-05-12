using System.Collections.Concurrent;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using SftpIngestion.Domain.Interfaces;

namespace SftpIngestion.Infrastructure.Sftp;

public class SftpConnectionPool : ISftpConnectionPool, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly ConcurrentDictionary<string, Queue<ISftpClientWrapper>> _pools = new();
    private readonly SecretClient _secretClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly int _maxPoolSize;

    public SftpConnectionPool(SecretClient secretClient, ILoggerFactory loggerFactory, int maxPoolSize = 5)
    {
        _secretClient = secretClient;
        _loggerFactory = loggerFactory;
        _maxPoolSize = maxPoolSize;
    }

    public async Task<ISftpClientWrapper> GetConnectionAsync(string clearinghouseId, CancellationToken cancellationToken = default)
    {
        var semaphore = _semaphores.GetOrAdd(clearinghouseId, _ => new SemaphoreSlim(_maxPoolSize, _maxPoolSize));
        await semaphore.WaitAsync(cancellationToken);

        var pool = _pools.GetOrAdd(clearinghouseId, _ => new Queue<ISftpClientWrapper>());

        lock (pool)
        {
            if (pool.Count > 0)
            {
                var existing = pool.Dequeue();
                if (existing.IsConnected) return existing;
            }
        }

        var client = await CreateClientAsync(clearinghouseId, cancellationToken);
        await client.ConnectAsync(cancellationToken);
        return client;
    }

    public Task ReleaseConnectionAsync(string clearinghouseId, ISftpClientWrapper client, CancellationToken cancellationToken = default)
    {
        var pool = _pools.GetOrAdd(clearinghouseId, _ => new Queue<ISftpClientWrapper>());
        lock (pool)
        {
            if (pool.Count < _maxPoolSize && client.IsConnected)
                pool.Enqueue(client);
        }

        if (_semaphores.TryGetValue(clearinghouseId, out var semaphore))
            semaphore.Release();

        return Task.CompletedTask;
    }

    public async Task InvalidateConnectionAsync(string clearinghouseId, CancellationToken cancellationToken = default)
    {
        if (_pools.TryRemove(clearinghouseId, out var pool))
        {
            lock (pool)
            {
                while (pool.Count > 0)
                {
                    var client = pool.Dequeue();
                    _ = client.DisposeAsync();
                }
            }
        }
        await Task.CompletedTask;
    }

    private async Task<ISftpClientWrapper> CreateClientAsync(string clearinghouseId, CancellationToken cancellationToken)
    {
        var host = (await _secretClient.GetSecretAsync($"Clearinghouses-{clearinghouseId}-Host", cancellationToken: cancellationToken)).Value.Value;
        var port = int.Parse((await _secretClient.GetSecretAsync($"Clearinghouses-{clearinghouseId}-Port", cancellationToken: cancellationToken)).Value.Value);
        var username = (await _secretClient.GetSecretAsync($"Clearinghouses-{clearinghouseId}-UserName", cancellationToken: cancellationToken)).Value.Value;
        var password = (await _secretClient.GetSecretAsync($"Clearinghouses-{clearinghouseId}-UserPassword", cancellationToken: cancellationToken)).Value.Value;

        return new SftpClientWrapper(host, port, username, password, _loggerFactory.CreateLogger<SftpClientWrapper>());
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pool in _pools.Values)
        {
            lock (pool)
            {
                while (pool.Count > 0)
                {
                    var client = pool.Dequeue();
                    _ = client.DisposeAsync();
                }
            }
        }
        await Task.CompletedTask;
    }
}
