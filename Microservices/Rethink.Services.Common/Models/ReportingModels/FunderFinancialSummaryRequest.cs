using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class FunderFinancialSummaryRequest : IValidatableObject
    {
        [Required]
        public int AccountInfoId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [RegularExpression("Transaction|Deposit", ErrorMessage = "DateType must be 'Transaction' or 'Deposit'.")]
        public string DateType { get; set; } = "Transaction";

        public List<int> LocationIds { get; set; } = new();

        public List<int> FunderIds { get; set; } = new();

        /// <summary>
        /// Optional: Filter by Rendering Provider IDs
        /// </summary>
        public List<int> RenderingProviderIds { get; set; } = new();

        /// <summary>
        /// Optional: Filter by Billing Provider IDs
        /// </summary>
        public List<int> BillingProviderIds { get; set; } = new();

        // Cross-field validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate < StartDate)
            {
                yield return new ValidationResult(
                    "EndDate must be greater than or equal to StartDate.",
                    new[] { nameof(EndDate) }
                );
            }
        }

        public List<string> LocationNames { get; set; } = new();
        public List<string> RenderingProviderNames { get; set; } = new();
        public List<string> BillingProviderNames { get; set; } = new();


    }
}
