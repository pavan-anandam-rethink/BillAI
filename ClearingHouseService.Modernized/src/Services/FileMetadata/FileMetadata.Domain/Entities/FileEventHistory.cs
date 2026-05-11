using ClearingHouse.SharedKernel.Domain;

namespace FileMetadata.Domain.Entities;

public class FileEventHistory : Entity
{
    public Guid FileId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }

    private FileEventHistory() { }

    public static FileEventHistory Create(Guid fileId, string eventType, string description, string correlationId, string? errorMessage = null)
    {
        return new FileEventHistory
        {
            FileId = fileId,
            EventType = eventType,
            Description = description,
            CorrelationId = correlationId,
            ErrorMessage = errorMessage,
            OccurredAt = DateTime.UtcNow
        };
    }
}
