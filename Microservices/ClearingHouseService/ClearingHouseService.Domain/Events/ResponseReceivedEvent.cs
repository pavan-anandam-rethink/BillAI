using ClearingHouseService.Domain.ValueObjects;

namespace ClearingHouseService.Domain.Events
{
    /// <summary>
    /// Domain event raised when a response has been received from a clearing house.
    /// </summary>
    public class ResponseReceivedEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public int ClearingHouseId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public EdiFormat Format { get; set; } = EdiFormat.Edi835;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CorrelationId { get; set; }
    }
}
