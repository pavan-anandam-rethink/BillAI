namespace ClaimLifecycleMonitoring.Contracts.Common;

/// <summary>
/// Uniform envelope describing a validation or domain error returned to API callers.
/// </summary>
public sealed class ApiErrorResponse
{
    /// <summary>Short machine readable error code.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Human readable error message.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>The correlation identifier for the request.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Optional list of validation failures.</summary>
    public IReadOnlyList<ValidationFailureDto> Failures { get; init; } = Array.Empty<ValidationFailureDto>();
}

/// <summary>
/// Represents a single field-level validation failure.
/// </summary>
public sealed class ValidationFailureDto
{
    /// <summary>Property/field that failed validation.</summary>
    public string Property { get; init; } = string.Empty;

    /// <summary>Human readable failure message.</summary>
    public string Message { get; init; } = string.Empty;
}
