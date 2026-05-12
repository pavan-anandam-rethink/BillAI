namespace SftpIngestion.Domain.Interfaces;

public interface ISftpConnectionPool
{
    Task<ISftpClientWrapper> GetConnectionAsync(string clearinghouseId, CancellationToken cancellationToken = default);
    Task ReleaseConnectionAsync(string clearinghouseId, ISftpClientWrapper client, CancellationToken cancellationToken = default);
    Task InvalidateConnectionAsync(string clearinghouseId, CancellationToken cancellationToken = default);
}
