using ClearingHouseService.Domain.ValueObjects;

namespace ClearingHouseService.Domain.Events
{
    /// <summary>
    /// Domain event raised when a claim has been submitted to a clearing house.
    /// </summary>
    public class ClaimSubmittedEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public int ClaimId { get; set; }
        public int ClearingHouseId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public EdiFormat Format { get; set; } = EdiFormat.Edi837P;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CorrelationId { get; set; }
    }
}
