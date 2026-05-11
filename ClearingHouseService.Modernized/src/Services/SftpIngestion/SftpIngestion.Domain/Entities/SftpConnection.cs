using ClearingHouse.SharedKernel.Domain;

namespace SftpIngestion.Domain.Entities;

public class SftpConnection : AggregateRoot
{
    public string Host { get; private set; } = string.Empty;
    public int Port { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string UploadDirectory { get; private set; } = string.Empty;
    public string DownloadDirectory { get; private set; } = string.Empty;
    public int ClearinghouseId { get; private set; }
    public string ClearinghouseName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime? LastPolledAt { get; private set; }
    public int ConsecutiveFailures { get; private set; }
    
    private SftpConnection() { }
    
    public static SftpConnection Create(string host, int port, string userName, string uploadDirectory, string downloadDirectory, int clearinghouseId, string clearinghouseName)
    {
        var connection = new SftpConnection
        {
            Host = host,
            Port = port,
            UserName = userName,
            UploadDirectory = uploadDirectory,
            DownloadDirectory = downloadDirectory,
            ClearinghouseId = clearinghouseId,
            ClearinghouseName = clearinghouseName,
            IsActive = true
        };
        return connection;
    }
    
    public void RecordSuccessfulPoll()
    {
        LastPolledAt = DateTime.UtcNow;
        ConsecutiveFailures = 0;
        IncrementVersion();
    }
    
    public void RecordFailedPoll()
    {
        LastPolledAt = DateTime.UtcNow;
        ConsecutiveFailures++;
        IncrementVersion();
    }
    
    public void Deactivate()
    {
        IsActive = false;
        IncrementVersion();
    }
    
    public void Activate()
    {
        IsActive = true;
        ConsecutiveFailures = 0;
        IncrementVersion();
    }
}
