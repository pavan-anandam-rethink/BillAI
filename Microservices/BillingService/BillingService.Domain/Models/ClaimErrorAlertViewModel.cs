using Rethink.Services.Common.Enums.Billing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class ClaimErrorAlertViewModel
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Source { get; set; }
        public string ErrorCode { get; set; }
        public string Description { get; set; }
        public List<string> CodeDescription { get; set; }
        public string Message { get; set; }
        public string AdjustmentLevel { get; set; }
        public string BatchId { get; set; }
        public string FileType { get; set; }
        public int? RefValidationId { get; set; }

        public DateTime? ResponseDate { get; set; }
        public ClaimErrorSource ClaimErrorSource { get; set; }
    }
}
