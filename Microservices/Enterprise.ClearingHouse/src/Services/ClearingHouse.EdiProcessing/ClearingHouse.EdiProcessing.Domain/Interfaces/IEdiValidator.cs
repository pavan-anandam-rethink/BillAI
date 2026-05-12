using ClearingHouse.EdiProcessing.Domain.Entities;
using ClearingHouse.EdiProcessing.Domain.Enums;

namespace ClearingHouse.EdiProcessing.Domain.Interfaces;

/// <summary>
/// Defines a validator for EDI segments.
/// </summary>
public interface IEdiValidator
{
    /// <summary>
    /// Validates an EDI segment asynchronously.
    /// </summary>
    /// <param name="segment">The EDI segment to validate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ValidationResult"/> containing the validation outcome.</returns>
    Task<ValidationResult> ValidateSegmentAsync(
        EdiSegment segment,
        CancellationToken cancellationToken);
}

/// <summary>
/// Represents the result of an EDI segment validation.
/// </summary>
/// <param name="IsValid">Whether the segment passed validation.</param>
/// <param name="Severity">The severity of the validation result.</param>
/// <param name="Errors">A read-only list of validation error messages.</param>
public sealed record ValidationResult(
    bool IsValid,
    ValidationSeverity Severity,
    IReadOnlyList<string> Errors);
