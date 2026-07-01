namespace ClaimLifecycleMonitoring.Application.Common.Exceptions;

/// <summary>
/// Thrown when application-level validation fails prior to invoking domain behaviour.
/// </summary>
public sealed class ApplicationValidationException : Exception
{
    /// <summary>Per-property validation failures.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>Creates a new <see cref="ApplicationValidationException"/>.</summary>
    public ApplicationValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
