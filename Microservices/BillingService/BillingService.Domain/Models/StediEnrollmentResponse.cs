using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class EnrollmentResponse
    {
        public List<EnrollmentItem> Items { get; set; }
    }
    [ExcludeFromCodeCoverage]
    public class EnrollmentItem
    {
        public string Id { get; set; }
        public Providers Provider { get; set; }
        public Payer Payer { get; set; }
        public Transactions Transactions { get; set; }
        public string Status { get; set; }
        public string Source { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime StatusLastUpdatedAt { get; set; }
        public List<History> History { get; set; }
        public List<object> Documents { get; set; }
    }
    [ExcludeFromCodeCoverage]
    public class Providers
    {
        public string Name { get; set; }
        public string Npi { get; set; }
        public string TaxId { get; set; }
        public string TaxIdType { get; set; }
        public string Id { get; set; }
    }
    [ExcludeFromCodeCoverage]
    public class Payer
    {
        public string StediPayerId { get; set; }
        public string? SubmittedPayerIdOrAlias { get; set; }
    }
    [ExcludeFromCodeCoverage]
    public class Transactions
    {
        public ClaimPayment ClaimPayment { get; set; }
    }
    [ExcludeFromCodeCoverage]
    public class ClaimPayment
    {
        public bool Enroll { get; set; }
    }
    [ExcludeFromCodeCoverage]
    public class History
    {
        public string? PreviousStatus { get; set; }
        public string NewStatus { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string Type { get; set; }
    }


}