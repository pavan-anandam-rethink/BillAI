namespace ClearingHouseService.Domain.Interfaces
{
    /// <summary>
    /// Validates EDI content before submission.
    /// </summary>
    public interface IEdiValidator
    {
        /// <summary>
        /// Validates EDI content for structural and business rule compliance.
        /// </summary>
        /// <param name="ediContent">The raw EDI content to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result with any errors found.</returns>
        Task<EdiValidationResult> ValidateAsync(string ediContent, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of EDI content validation.
    /// </summary>
    public class EdiValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public static EdiValidationResult Valid() => new() { IsValid = true };

        public static EdiValidationResult Invalid(params string[] errors)
        {
            return new EdiValidationResult
            {
                IsValid = false,
                Errors = errors.ToList()
            };
        }
    }
}
