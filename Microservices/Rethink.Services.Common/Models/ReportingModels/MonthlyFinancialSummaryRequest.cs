using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rethink.Services.Common.Models.ReportingModels
{
    public class MonthlyFinancialSummaryRequest : IValidatableObject
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

        // Cross-field validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate > EndDate)
            {
                yield return new ValidationResult(
                    "StartDate must be less than or equal to EndDate.",
                    new[] { nameof(StartDate), nameof(EndDate) });
            }
        }
        public List<string> LocationNames { get; set; } = new();
    }
}
