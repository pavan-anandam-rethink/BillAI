using ClearingHouse.SharedKernel.Domain;

namespace FileTracking.Domain.Entities;

public class FileTimeline : Entity
{
    public Guid TrackingRecordId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }

    private FileTimeline() { }

    public static FileTimeline Create(Guid trackingRecordId, string eventType, string description)
    {
        return new FileTimeline
        {
            TrackingRecordId = trackingRecordId,
            EventType = eventType,
            Description = description,
            OccurredAt = DateTime.UtcNow
        };
    }
}
