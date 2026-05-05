using System;

namespace BillingService.Domain.Models
{
    public class ClaimFiltersModel
    {
        public string ClaimNumber { get; set; }
        public string ClaimIds { get; set; }
        public string PatientIds { get; set; }
        public string ReasonCode { get; set; }
        public string FunderIds { get; set; }
        public string AssigneeIds { get; set; }
        public string LocationIds { get; set; }
        public string? ReasonIds { get; set; }
        public decimal? BalanceFrom { get; set; }
        public decimal? BalanceTo { get; set; }
        public decimal? BilledFrom { get; set; }
        public decimal? BilledTo { get; set; }
        public decimal? PatientResponsibilityFrom { get; set; }
        public decimal? PatientResponsibilityTo { get; set; }
        public DateTime? DateOfServiceFrom { get; set; } = null;
        public DateTime? DateOfServiceTo { get; set; } = null;
        public string RenderingProviderIds { get; set; }
        public string StatusIds { get; set; }
        public int Tab { get; set; }
        public bool ShowVoided { get; set; }
        public string ValidationIds { get; set; }
        public string ResponseIds { get; set; }
    }
}