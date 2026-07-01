namespace ClaimLifecycleMonitoring.Domain.Exceptions;

/// <summary>
/// Thrown when a domain invariant or validation rule is violated.
/// </summary>
public sealed class DomainValidationException : Exception
{
    /// <summary>Creates a new <see cref="DomainValidationException"/>.</summary>
    public DomainValidationException(string message) : base(message) { }

    /// <summary>Creates a new <see cref="DomainValidationException"/> with an inner exception.</summary>
    public DomainValidationException(string message, Exception inner) : base(message, inner) { }
}
