using System;

namespace BillingService.Domain.Models
{
    public class ClearinghouseCredentialValidationResult
    {
        public string ClearinghouseName { get; set; }
        public int ClearinghouseId { get; set; }
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ValidatedAt { get; set; }
        public long DurationMs { get; set; }
    }
}
