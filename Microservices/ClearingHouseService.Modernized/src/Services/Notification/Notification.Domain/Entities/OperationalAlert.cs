using ClearingHouse.SharedKernel.Domain;

namespace Notification.Domain.Entities;

public class OperationalAlert : AggregateRoot<Guid>
{
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public AlertSeverity Severity { get; private set; }
    public AlertCategory Category { get; private set; }
    public string SourceService { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public bool IsAcknowledged { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public string? AcknowledgedBy { get; private set; }
    public IDictionary<string, string> Properties { get; private set; } = new Dictionary<string, string>();

    private OperationalAlert() { }

    public static OperationalAlert Create(string title, string message, AlertSeverity severity,
        AlertCategory category, string sourceService, string correlationId, IDictionary<string, string>? properties = null)
    {
        return new OperationalAlert
        {
            Id = Guid.NewGuid(),
            Title = title,
            Message = message,
            Severity = severity,
            Category = category,
            SourceService = sourceService,
            CorrelationId = correlationId,
            Properties = properties ?? new Dictionary<string, string>()
        };
    }

    public void Acknowledge(string userId)
    {
        IsAcknowledged = true;
        AcknowledgedAt = DateTime.UtcNow;
        AcknowledgedBy = userId;
        IncrementVersion();
    }
}

public enum AlertSeverity { Information = 0, Warning = 1, Error = 2, Critical = 3 }
public enum AlertCategory { SftpConnection, FileProcessing, BatchFailed, DlqThreshold, ReconciliationFailure, SystemHealth }
