using ClearingHouse.SharedKernel.Domain;

namespace SftpIngestion.Domain.Entities;

public class SftpConnection : AggregateRoot<Guid>
{
    public string ClearinghouseId { get; private set; } = string.Empty;
    public string Host { get; private set; } = string.Empty;
    public int Port { get; private set; } = 22;
    public string Username { get; private set; } = string.Empty;
    public string UploadDirectory { get; private set; } = string.Empty;
    public string DownloadDirectory { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime? LastPolledAt { get; private set; }
    public int PollingIntervalSeconds { get; private set; } = 300;
    public int MaxRetryAttempts { get; private set; } = 3;
    public int ConnectionTimeoutSeconds { get; private set; } = 30;

    private SftpConnection() { }

    public static SftpConnection Create(
        string clearinghouseId, string host, int port,
        string username, string uploadDirectory, string downloadDirectory,
        int pollingIntervalSeconds = 300, int maxRetryAttempts = 3, int connectionTimeoutSeconds = 30)
    {
        var connection = new SftpConnection
        {
            Id = Guid.NewGuid(),
            ClearinghouseId = clearinghouseId,
            Host = host,
            Port = port,
            Username = username,
            UploadDirectory = uploadDirectory,
            DownloadDirectory = downloadDirectory,
            PollingIntervalSeconds = pollingIntervalSeconds,
            MaxRetryAttempts = maxRetryAttempts,
            ConnectionTimeoutSeconds = connectionTimeoutSeconds
        };

        connection.AddDomainEvent(new SftpConnectionCreatedEvent(connection.Id, clearinghouseId));
        return connection;
    }

    public void MarkPolled()
    {
        LastPolledAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public void Deactivate()
    {
        IsActive = false;
        IncrementVersion();
    }
}

public record SftpConnectionCreatedEvent(Guid ConnectionId, string ClearinghouseId)
    : DomainEventBase;
