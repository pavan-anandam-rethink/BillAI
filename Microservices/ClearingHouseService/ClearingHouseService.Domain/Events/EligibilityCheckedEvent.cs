namespace ClearingHouseService.Domain.Events
{
    /// <summary>
    /// Domain event raised when an eligibility check (270/271) has been completed.
    /// </summary>
    public class EligibilityCheckedEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public int ClearingHouseId { get; set; }
        public string? Edi270FileName { get; set; }
        public string? Edi271FileName { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CorrelationId { get; set; }
    }
}
