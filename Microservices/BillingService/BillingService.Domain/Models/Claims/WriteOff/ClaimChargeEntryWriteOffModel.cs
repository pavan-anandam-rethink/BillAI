using System;

namespace BillingService.Domain.Models.Claims
{
    public class ClaimChargeEntryWriteOffModel
    {
        public int Id { get; set; }
        public int WriteOffReasonCodeId { get; set; }
        public decimal? WriteOffAmount { get; set; }
        public string? Description { get; set; }
        public DateTime? DateLastModified { get; set; }
    }
}
